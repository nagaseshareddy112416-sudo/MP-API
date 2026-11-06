using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;
using GfcEngine.Infrastructure.Services;
using Xunit;

namespace GfcEngine.Tests;

public class ClashDetectionTests
{
    private static readonly GeometryFactory _factory = new(new PrecisionModel(), 32644);

    [Fact]
    public async Task DetectClashes_SewerVsWater_DetectsClashWhenVerticalGapIsBelowTolerance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var transformer = new ProjNetCrsTransformer();
        var clashService = new ClashDetectionService(context, transformer, NullLogger<ClashDetectionService>.Instance);

        var wsId = Guid.NewGuid();
        var drawingWaterId = Guid.NewGuid();
        var drawingSewerId = Guid.NewGuid();
        var revWaterId = Guid.NewGuid();
        var revSewerId = Guid.NewGuid();

        // Water main line from (-500, 8) to (500, 8) at depth 1.5m
        var waterGeom = _factory.CreateLineString(new[]
        {
            new Coordinate(448350 - 500, 1825744 + 8),
            new Coordinate(448350 + 500, 1825744 + 8)
        });

        // Sewer line from (0, -200) to (0, 200) at depth 1.7m (crosses water at (0, 8) with depth diff 0.2m)
        var sewerGeom = _factory.CreateLineString(new[]
        {
            new Coordinate(448350, 1825744 - 200),
            new Coordinate(448350, 1825744 + 200)
        });

        var waterFeature = new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = drawingWaterId,
            RevisionId = revWaterId,
            CadLayer = "WATER_MAIN",
            Category = "Utility",
            SubType = "Water",
            NativeGeometry = waterGeom,
            DisplayGeometry = transformer.Transform(waterGeom, 32644, 4326),
            DepthM = 1.5,
            IsActive = true
        };

        var sewerFeature = new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = drawingSewerId,
            RevisionId = revSewerId,
            CadLayer = "SEWER_LINE",
            Category = "Utility",
            SubType = "Sewer",
            NativeGeometry = sewerGeom,
            DisplayGeometry = transformer.Transform(sewerGeom, 32644, 4326),
            DepthM = 1.7,
            IsActive = false
        };

        context.ExtractedFeatures.AddRange(waterFeature, sewerFeature);
        await context.SaveChangesAsync();

        // Act - vertical tolerance = 0.5 m
        var clashes = await clashService.DetectClashesForRevisionAsync(revSewerId, wsId, 0.5);

        // Assert
        clashes.Should().HaveCount(1);
        var clash = clashes.First();
        clash.VerticalGapM.Should().BeApproximately(0.2, 0.01);
        clash.Status.Should().Be(ClashStatus.Open);
    }

    [Fact]
    public async Task DetectClashes_StormVsSewer_NoClashWhenGapExceedsTolerance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var transformer = new ProjNetCrsTransformer();
        var clashService = new ClashDetectionService(context, transformer, NullLogger<ClashDetectionService>.Instance);

        var wsId = Guid.NewGuid();
        var revSewerId = Guid.NewGuid();
        var revStormId = Guid.NewGuid();

        // Sewer line at depth 1.7m
        var sewerGeom = _factory.CreateLineString(new[]
        {
            new Coordinate(448350, 1825744 - 200),
            new Coordinate(448350, 1825744 + 200)
        });

        // Storm drain line at depth 3.5m (crosses sewer at (0, -12), gap = 1.8m)
        var stormGeom = _factory.CreateLineString(new[]
        {
            new Coordinate(448350 - 300, 1825744 - 12),
            new Coordinate(448350 + 300, 1825744 - 12)
        });

        var sewerFeature = new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = Guid.NewGuid(),
            RevisionId = revSewerId,
            CadLayer = "SEWER_LINE",
            Category = "Utility",
            SubType = "Sewer",
            NativeGeometry = sewerGeom,
            DisplayGeometry = transformer.Transform(sewerGeom, 32644, 4326),
            DepthM = 1.7,
            IsActive = true
        };

        var stormFeature = new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = Guid.NewGuid(),
            RevisionId = revStormId,
            CadLayer = "STORM_DRAIN",
            Category = "Utility",
            SubType = "Storm drain",
            NativeGeometry = stormGeom,
            DisplayGeometry = transformer.Transform(stormGeom, 32644, 4326),
            DepthM = 3.5,
            IsActive = false
        };

        context.ExtractedFeatures.AddRange(sewerFeature, stormFeature);
        await context.SaveChangesAsync();

        // Act - vertical tolerance = 0.5 m
        var clashes = await clashService.DetectClashesForRevisionAsync(revStormId, wsId, 0.5);

        // Assert - Gap is 1.8m > 0.5m, so NO clash should be detected!
        clashes.Should().BeEmpty();
    }
}
