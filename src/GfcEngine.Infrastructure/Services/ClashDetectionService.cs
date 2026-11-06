using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.Infrastructure.Services;

public class ClashDetectionService : IClashDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ICrsTransformer _transformer;
    private readonly ILogger<ClashDetectionService> _logger;

    public ClashDetectionService(
        ApplicationDbContext context,
        ICrsTransformer transformer,
        ILogger<ClashDetectionService> logger)
    {
        _context = context;
        _transformer = transformer;
        _logger = logger;
    }

    public async Task<List<Clash>> DetectClashesForRevisionAsync(Guid revisionId, Guid workspaceId, double verticalToleranceM, CancellationToken cancellationToken = default)
    {
        var revisionFeatures = await _context.ExtractedFeatures
            .Where(f => f.RevisionId == revisionId)
            .ToListAsync(cancellationToken);

        if (revisionFeatures.Count == 0)
        {
            return new List<Clash>();
        }

        // Target utilities or building footprints
        var newUtilities = revisionFeatures
            .Where(f => f.Category.Equals("Utility", StringComparison.OrdinalIgnoreCase) ||
                        f.Category.Equals("Building", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (newUtilities.Count == 0)
        {
            return new List<Clash>();
        }

        // Compare against active features from approved revisions in the same workspace (excluding this revision)
        var activeFeatures = await _context.ExtractedFeatures
            .Where(f => f.WorkspaceId == workspaceId && f.IsActive && f.RevisionId != revisionId &&
                       (f.Category == "Utility" || f.Category == "Building"))
            .ToListAsync(cancellationToken);

        // Also check pairwise within newUtilities (in case multiple utilities in the same drawing cross)
        var detectedClashes = new List<Clash>();

        // 1. Compare newUtilities against existing activeFeatures
        foreach (var featA in newUtilities)
        {
            foreach (var featB in activeFeatures)
            {
                CheckPair(featA, featB, detectedClashes, verticalToleranceM, workspaceId);
            }
        }

        // 2. Pairwise within newUtilities
        for (int i = 0; i < newUtilities.Count; i++)
        {
            for (int j = i + 1; j < newUtilities.Count; j++)
            {
                CheckPair(newUtilities[i], newUtilities[j], detectedClashes, verticalToleranceM, workspaceId);
            }
        }

        if (detectedClashes.Count > 0)
        {
            _context.Clashes.AddRange(detectedClashes);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Detected {Count} 3D clashes for revision {RevisionId}", detectedClashes.Count, revisionId);
        }

        return detectedClashes;
    }

    private void CheckPair(ExtractedFeature featA, ExtractedFeature featB, List<Clash> result, double toleranceM, Guid workspaceId)
    {
        // Must intersect horizontally in 2D
        if (!featA.NativeGeometry.Intersects(featB.NativeGeometry))
        {
            return;
        }

        var intersection = featA.NativeGeometry.Intersection(featB.NativeGeometry);
        if (intersection.IsEmpty)
        {
            return;
        }

        var intersectPoint = intersection.Centroid;

        // Vertical clash test
        double depthA = featA.DepthM ?? (featA.Z.HasValue ? Math.Abs(featA.Z.Value) : 0.0);
        double depthB = featB.DepthM ?? (featB.Z.HasValue ? Math.Abs(featB.Z.Value) : 0.0);
        double gap = Math.Abs(depthA - depthB);

        // If gap is within vertical clearance tolerance (e.g. <= 0.5 m), it's a 3D clash!
        if (gap <= toleranceM)
        {
            var displayPoint = _transformer.Transform(intersectPoint, 32644, 4326);

            var clash = new Clash
            {
                WorkspaceId = workspaceId,
                FeatureAId = featA.Id,
                FeatureBId = featB.Id,
                DrawingAId = featA.DrawingId,
                DrawingBId = featB.DrawingId,
                Location = intersectPoint,
                DisplayLocation = displayPoint,
                VerticalGapM = Math.Round(gap, 2),
                Status = ClashStatus.Open,
                Notes = $"Vertical clearance conflict: {featA.SubType} (depth {depthA:F1}m) crosses {featB.SubType} (depth {depthB:F1}m) with a gap of {gap:F2}m (tolerance: {toleranceM:F1}m)",
                DetectedAt = DateTime.UtcNow
            };

            result.Add(clash);
        }
    }
}
