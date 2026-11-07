using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm.Distance;
using NetTopologySuite.Geometries;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.Infrastructure.Services;

public class ComplianceEngineService : IComplianceEngineService
{
    private readonly ApplicationDbContext _context;
    private readonly IFeatureMatchingService _matchingService;

    public ComplianceEngineService(
        ApplicationDbContext context,
        IFeatureMatchingService matchingService)
    {
        _context = context;
        _matchingService = matchingService;
    }

    public async Task<List<ComplianceResult>> EvaluateComplianceAsync(
        Guid workspaceId,
        Guid asBuiltDrawingId,
        CancellationToken ct = default)
    {
        var asBuiltDrawing = await _context.UploadedDrawings
            .FirstOrDefaultAsync(d => d.Id == asBuiltDrawingId, ct);

        if (asBuiltDrawing == null)
            throw new ArgumentException($"As-built drawing {asBuiltDrawingId} not found");

        if (!asBuiltDrawing.GfcDrawingId.HasValue)
            throw new InvalidOperationException("As-built drawing is not linked to a baseline GFC drawing.");

        var gfcDrawingId = asBuiltDrawing.GfcDrawingId.Value;
        var gfcDrawing = await _context.UploadedDrawings
            .FirstOrDefaultAsync(d => d.Id == gfcDrawingId, ct);

        if (gfcDrawing == null)
            throw new ArgumentException($"Baseline GFC drawing {gfcDrawingId} not found");

        // Determine GFC revision used (latest active or current)
        var gfcRevision = await _context.DrawingRevisions
            .Where(r => r.DrawingId == gfcDrawingId && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct) ??
            await _context.DrawingRevisions
            .Where(r => r.DrawingId == gfcDrawingId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var gfcRevId = gfcRevision?.Id ?? Guid.Empty;

        // Perform feature matching
        var matches = await _matchingService.MatchFeaturesAsync(workspaceId, asBuiltDrawingId, gfcDrawingId, 5.0, ct);

        // Fetch tolerance rules for workspace
        var toleranceRules = await _context.ToleranceRules
            .Where(r => r.WorkspaceId == workspaceId)
            .ToListAsync(ct);

        // Clear existing compliance results for this drawing
        var existingResults = await _context.ComplianceResults
            .Where(r => r.WorkspaceId == workspaceId && r.AsBuiltDrawingId == asBuiltDrawingId)
            .Include(r => r.Deviations)
            .ToListAsync(ct);

        if (existingResults.Any())
        {
            _context.ComplianceResults.RemoveRange(existingResults);
            await _context.SaveChangesAsync(ct);
        }

        var results = new List<ComplianceResult>();
        var matchedAsBuiltFeatureIds = new HashSet<Guid>();
        var matchedGfcFeatureIds = new HashSet<Guid>();

        // 1. Process matched feature pairs
        foreach (var match in matches)
        {
            if (!match.AsBuiltFeatureId.HasValue || !match.GfcFeatureId.HasValue)
                continue;

            matchedAsBuiltFeatureIds.Add(match.AsBuiltFeatureId.Value);
            matchedGfcFeatureIds.Add(match.GfcFeatureId.Value);

            var ab = await _context.ExtractedFeatures.FindAsync(new object[] { match.AsBuiltFeatureId.Value }, ct);
            var gfc = await _context.ExtractedFeatures.FindAsync(new object[] { match.GfcFeatureId.Value }, ct);

            if (ab == null || gfc == null)
                continue;

            // Find applicable tolerance rule
            var rule = toleranceRules.FirstOrDefault(r =>
                string.Equals(r.Category, ab.Category, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.SubType, ab.SubType, StringComparison.OrdinalIgnoreCase)) ??
                toleranceRules.FirstOrDefault(r =>
                string.Equals(r.Category, ab.Category, StringComparison.OrdinalIgnoreCase)) ??
                new ToleranceRule { HorizontalToleranceM = 0.10, VerticalToleranceM = 0.05, LengthToleranceM = 0.50 };

            // Horizontal deviation (Discrete Hausdorff Distance)
            double maxHorizontalDev = 0;
            double avgHorizontalDev = 0;

            if (ab.NativeGeometry != null && gfc.NativeGeometry != null)
            {
                try
                {
                    maxHorizontalDev = DiscreteHausdorffDistance.Distance(ab.NativeGeometry, gfc.NativeGeometry);
                    // Approximate average as centroid distance
                    avgHorizontalDev = ab.NativeGeometry.Centroid.Distance(gfc.NativeGeometry.Centroid);
                }
                catch
                {
                    maxHorizontalDev = ab.NativeGeometry.Distance(gfc.NativeGeometry);
                    avgHorizontalDev = maxHorizontalDev;
                }
            }

            // Vertical deviation
            double verticalDev = 0;
            if (ab.DepthM.HasValue && gfc.DepthM.HasValue)
            {
                verticalDev = Math.Abs(ab.DepthM.Value - gfc.DepthM.Value);
            }
            else if (ab.Z.HasValue && gfc.Z.HasValue)
            {
                verticalDev = Math.Abs(ab.Z.Value - gfc.Z.Value);
            }
            else if (ab.NativeGeometry?.Coordinate != null && gfc.NativeGeometry?.Coordinate != null &&
                     !double.IsNaN(ab.NativeGeometry.Coordinate.Z) && !double.IsNaN(gfc.NativeGeometry.Coordinate.Z))
            {
                verticalDev = Math.Abs(ab.NativeGeometry.Coordinate.Z - gfc.NativeGeometry.Coordinate.Z);
            }

            // Length deviation
            double lengthDev = 0;
            if (ab.NativeGeometry != null && gfc.NativeGeometry != null)
            {
                lengthDev = Math.Abs(ab.NativeGeometry.Length - gfc.NativeGeometry.Length);
            }

            // Attribute differences
            var diffs = new Dictionary<string, object>();
            if (ab.DiameterMm.HasValue && gfc.DiameterMm.HasValue && ab.DiameterMm.Value != gfc.DiameterMm.Value)
            {
                diffs["DiameterMm"] = new { Designed = gfc.DiameterMm.Value, Built = ab.DiameterMm.Value };
            }
            if (!string.IsNullOrEmpty(ab.Material) && !string.IsNullOrEmpty(gfc.Material) &&
                !string.Equals(ab.Material, gfc.Material, StringComparison.OrdinalIgnoreCase))
            {
                diffs["Material"] = new { Designed = gfc.Material, Built = ab.Material };
            }

            // Classification
            ComplianceStatus status;
            bool isZeroTolerance = maxHorizontalDev < 0.001 && verticalDev < 0.001 && lengthDev < 0.01 && diffs.Count == 0;
            bool withinTolerance = maxHorizontalDev <= rule.HorizontalToleranceM &&
                                   verticalDev <= rule.VerticalToleranceM &&
                                   lengthDev <= rule.LengthToleranceM &&
                                   diffs.Count == 0;

            if (isZeroTolerance)
            {
                status = ComplianceStatus.Compliant;
            }
            else if (withinTolerance)
            {
                status = ComplianceStatus.WithinTolerance;
            }
            else
            {
                status = ComplianceStatus.Deviation;
            }

            var compResult = new ComplianceResult
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                AsBuiltDrawingId = asBuiltDrawingId,
                GfcDrawingId = gfcDrawingId,
                GfcRevisionId = gfcRevId,
                FeatureMatchId = match.Id,
                AsBuiltFeatureId = ab.Id,
                GfcFeatureId = gfc.Id,
                Category = ab.Category,
                SubType = ab.SubType,
                ChainageStart = ab.Chainage ?? gfc.Chainage,
                ChainageEnd = null,
                HorizontalDeviationM = Math.Round(maxHorizontalDev, 4),
                AverageHorizontalDeviationM = Math.Round(avgHorizontalDev, 4),
                VerticalDeviationM = Math.Round(verticalDev, 4),
                LengthDeviationM = Math.Round(lengthDev, 4),
                AttributeDiffsJson = diffs.Any() ? JsonSerializer.Serialize(diffs) : null,
                Status = status,
                ComputedAt = DateTime.UtcNow
            };

            // If Deviation, register pending deviation workflow item
            if (status == ComplianceStatus.Deviation)
            {
                compResult.Deviations.Add(new Deviation
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    ComplianceResultId = compResult.Id,
                    Decision = DeviationDecision.Pending,
                    Reason = null
                });
            }

            results.Add(compResult);
        }

        // 2. Unmatched GFC features = Missing
        var gfcFeatures = await _context.ExtractedFeatures
            .Where(f => f.DrawingId == gfcDrawingId)
            .ToListAsync(ct);

        foreach (var gfc in gfcFeatures)
        {
            if (matchedGfcFeatureIds.Contains(gfc.Id))
                continue;

            var missingResult = new ComplianceResult
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                AsBuiltDrawingId = asBuiltDrawingId,
                GfcDrawingId = gfcDrawingId,
                GfcRevisionId = gfcRevId,
                AsBuiltFeatureId = null,
                GfcFeatureId = gfc.Id,
                Category = gfc.Category,
                SubType = gfc.SubType,
                ChainageStart = gfc.Chainage,
                Status = ComplianceStatus.Missing,
                ComputedAt = DateTime.UtcNow
            };

            missingResult.Deviations.Add(new Deviation
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                ComplianceResultId = missingResult.Id,
                Decision = DeviationDecision.Pending,
                Reason = "Designed GFC feature omitted in as-built submission (Missing)"
            });

            results.Add(missingResult);
        }

        // 3. Unmatched As-Built features = Extra
        var asBuiltFeatures = await _context.ExtractedFeatures
            .Where(f => f.DrawingId == asBuiltDrawingId)
            .ToListAsync(ct);

        foreach (var ab in asBuiltFeatures)
        {
            if (matchedAsBuiltFeatureIds.Contains(ab.Id))
                continue;

            var extraResult = new ComplianceResult
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                AsBuiltDrawingId = asBuiltDrawingId,
                GfcDrawingId = gfcDrawingId,
                GfcRevisionId = gfcRevId,
                AsBuiltFeatureId = ab.Id,
                GfcFeatureId = null,
                Category = ab.Category,
                SubType = ab.SubType,
                ChainageStart = ab.Chainage,
                Status = ComplianceStatus.Extra,
                ComputedAt = DateTime.UtcNow
            };

            extraResult.Deviations.Add(new Deviation
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                ComplianceResultId = extraResult.Id,
                Decision = DeviationDecision.Pending,
                Reason = "Unplanned feature built without baseline GFC design (Extra)"
            });

            results.Add(extraResult);
        }

        _context.ComplianceResults.AddRange(results);
        await _context.SaveChangesAsync(ct);

        return results;
    }
}

