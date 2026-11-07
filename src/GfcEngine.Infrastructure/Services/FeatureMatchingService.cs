using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.Infrastructure.Services;

public class FeatureMatchingService : IFeatureMatchingService
{
    private readonly ApplicationDbContext _context;

    public FeatureMatchingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FeatureMatch>> MatchFeaturesAsync(
        Guid workspaceId,
        Guid asBuiltDrawingId,
        Guid gfcDrawingId,
        double searchDistanceM = 5.0,
        CancellationToken ct = default)
    {
        // Remove existing automatic matches for this pair to support re-evaluation
        var existingMatches = await _context.FeatureMatches
            .Where(m => m.WorkspaceId == workspaceId &&
                        m.AsBuiltDrawingId == asBuiltDrawingId &&
                        m.GfcDrawingId == gfcDrawingId &&
                        m.MatchType == FeatureMatchType.Automatic)
            .ToListAsync(ct);

        if (existingMatches.Any())
        {
            _context.FeatureMatches.RemoveRange(existingMatches);
            await _context.SaveChangesAsync(ct);
        }

        var asBuiltFeatures = await _context.ExtractedFeatures
            .Where(f => f.DrawingId == asBuiltDrawingId)
            .ToListAsync(ct);

        var gfcFeatures = await _context.ExtractedFeatures
            .Where(f => f.DrawingId == gfcDrawingId)
            .ToListAsync(ct);

        var matches = new List<FeatureMatch>();
        var matchedGfcIds = new HashSet<Guid>();
        var matchedAsBuiltIds = new HashSet<Guid>();

        // Step 1: Match by Category, SubType, and spatial proximity / chainage
        foreach (var ab in asBuiltFeatures)
        {
            // Candidate GFC features with same Category and SubType
            var candidates = gfcFeatures
                .Where(g => !matchedGfcIds.Contains(g.Id) &&
                            string.Equals(g.Category, ab.Category, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(g.SubType, ab.SubType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!candidates.Any())
            {
                // Fallback: candidate GFC features with same Category
                candidates = gfcFeatures
                    .Where(g => !matchedGfcIds.Contains(g.Id) &&
                                string.Equals(g.Category, ab.Category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ExtractedFeature? bestMatch = null;
            double minDistance = double.MaxValue;

            foreach (var cand in candidates)
            {
                // Check spatial distance between NativeGeometry
                double dist = double.MaxValue;
                if (ab.NativeGeometry != null && cand.NativeGeometry != null)
                {
                    dist = ab.NativeGeometry.Distance(cand.NativeGeometry);
                }

                // If chainages match / overlap, give high priority
                bool chainageMatch = false;
                if (ab.Chainage.HasValue && cand.Chainage.HasValue && Math.Abs(ab.Chainage.Value - cand.Chainage.Value) < 20.0)
                {
                    chainageMatch = true;
                }

                if ((dist <= searchDistanceM || chainageMatch) && dist < minDistance)
                {
                    minDistance = dist;
                    bestMatch = cand;
                }
            }

            if (bestMatch != null)
            {
                matchedGfcIds.Add(bestMatch.Id);
                matchedAsBuiltIds.Add(ab.Id);

                var match = new FeatureMatch
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    AsBuiltDrawingId = asBuiltDrawingId,
                    GfcDrawingId = gfcDrawingId,
                    AsBuiltFeatureId = ab.Id,
                    GfcFeatureId = bestMatch.Id,
                    MatchType = FeatureMatchType.Automatic,
                    MatchedAt = DateTime.UtcNow
                };
                matches.Add(match);
            }
        }

        if (matches.Any())
        {
            _context.FeatureMatches.AddRange(matches);
            await _context.SaveChangesAsync(ct);
        }

        return matches;
    }
}
