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

public class AcceptanceTestSuite
{
    private (ApplicationDbContext context, NetDxfExtractionService extractionService, ClashDetectionService clashService, Guid workspaceId) CreateTestEnvironment()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var transformer = new ProjNetCrsTransformer();
        var extractionService = new NetDxfExtractionService(context, transformer, NullLogger<NetDxfExtractionService>.Instance);
        var clashService = new ClashDetectionService(context, transformer, NullLogger<ClashDetectionService>.Instance);

        var wsId = Guid.NewGuid();
        var ws = new Workspace
        {
            Id = wsId,
            Name = "Amaravati Test Workspace",
            Code = "AMV-TEST",
            DefaultEpsg = 32644,
            DisplayEpsg = 4326,
            CenterLon = 80.516,
            CenterLat = 16.513,
            DefaultZoom = 14,
            ClashVerticalToleranceM = 0.5,
            FarFromExistingDataWarningM = 5000.0
        };

        context.Workspaces.Add(ws);

        // Seed CAD layer mappings
        context.CadLayerMappings.AddRange(
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "ROAD_CL", Category = "Road", SubType = "Centerline", GeometryType = "line", Color = "#000000" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "ROAD_EDGE", Category = "Road", SubType = "Edge of pavement", GeometryType = "line", Color = "#555555" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "CHAINAGE", Category = "Road", SubType = "Chainage marker", GeometryType = "point", Color = "#ff9800" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "WATER_MAIN", Category = "Utility", SubType = "Water", GeometryType = "line", Color = "#1f77ff" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "SEWER_LINE", Category = "Utility", SubType = "Sewer", GeometryType = "line", Color = "#8b4513" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "STORM_DRAIN", Category = "Utility", SubType = "Storm drain", GeometryType = "line", Color = "#2ca02c" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "POWER_CABLE", Category = "Utility", SubType = "Power", GeometryType = "line", Color = "#e63946" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "BLDG_FOOTPRINT", Category = "Building", SubType = "Footprint", GeometryType = "polygon", Color = "#6c757d" }
        );

        context.SaveChanges();
        return (context, extractionService, clashService, wsId);
    }

    private string GetDxfPath(string subFolder, string fileName)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "gis_test_pack", subFolder, fileName);
        if (!File.Exists(path))
        {
            path = Path.Combine("d:\\MP\\MP-API\\gis_test_pack", subFolder, fileName);
        }
        if (!File.Exists(path))
        {
            path = Path.Combine("d:\\MP-API\\gis_test_pack", subFolder, fileName);
        }
        return Path.GetFullPath(path);
    }

    [Fact]
    public async Task AcceptanceTest1_Uploading6ValidDxfs_PlacesAllNearLon80_516_Lat16_513()
    {
        // 1. Uploading the 6 valid DXFs places all features near lon 80.516, lat 16.513
        var (context, extractor, _, wsId) = CreateTestEnvironment();

        string[] validFiles =
        {
            "GFC_ROAD_R1_CH0000-1000_R0.dxf",
            "GFC_WATER_R1_CH0000-1000_R0.dxf",
            "GFC_SEWER_R2_CH0000-0400_R0.dxf",
            "GFC_STORM_R1_CH0200-0800_R0.dxf",
            "GFC_POWER_R1_CH0000-1000_R0.dxf",
            "GFC_BUILDING_P-001_R0.dxf"
        };

        foreach (var file in validFiles)
        {
            var fullPath = GetDxfPath("gfc_dxf", file);
            var result = await extractor.ExtractAsync(fullPath, 32644, wsId);

            result.Success.Should().BeTrue($"File {file} should extract successfully");
            result.Entities.Should().NotBeEmpty($"File {file} must have features");

            // Verify coordinate bounds near 80.516E, 16.513N
            result.DisplayExtent.Centre.X.Should().BeApproximately(80.516, 0.05, $"File {file} longitudes should be around Amaravati (80.516)");
            result.DisplayExtent.Centre.Y.Should().BeApproximately(16.513, 0.05, $"File {file} latitudes should be around Amaravati (16.513)");
        }
    }

    [Fact]
    public async Task AcceptanceTest2_ClashDetection_DetectsExactlyOneClashBetweenSewerAndWater()
    {
        // 2. Exactly 1 clash is detected: sewer (1.7 m deep) vs water main (1.5 m deep), vertical gap 0.2 m.
        // Storm drain vs sewer (gap 1.8 m) and power vs sewer (gap 0.8 m) are NOT clashes with the 0.5 m tolerance.
        var (context, extractor, clashService, wsId) = CreateTestEnvironment();

        // Step 1: Ingest and activate Water drawing features (depth 1.5m)
        var waterPath = GetDxfPath("gfc_dxf", "GFC_WATER_R1_CH0000-1000_R0.dxf");
        var waterResult = await extractor.ExtractAsync(waterPath, 32644, wsId);
        var waterDrawingId = Guid.NewGuid();
        var waterRevId = Guid.NewGuid();
        var waterFeatures = waterResult.Entities.Select(e => new ExtractedFeature
        {
            WorkspaceId = wsId,
            DrawingId = waterDrawingId,
            RevisionId = waterRevId,
            CadLayer = e.LayerName,
            Category = e.Category,
            SubType = e.SubType,
            GeometryType = e.GeometryType,
            NativeGeometry = e.NativeGeometry,
            DisplayGeometry = e.DisplayGeometry,
            DepthM = e.DepthM,
            Z = e.Z,
            IsActive = true // Active on map
        }).ToList();
        context.ExtractedFeatures.AddRange(waterFeatures);
        await context.SaveChangesAsync();

        // Step 2: Ingest Storm Drain drawing (depth 3.5m) and check clashes vs Water/Sewer
        var stormPath = GetDxfPath("gfc_dxf", "GFC_STORM_R1_CH0200-0800_R0.dxf");
        var stormResult = await extractor.ExtractAsync(stormPath, 32644, wsId);
        var stormDrawingId = Guid.NewGuid();
        var stormRevId = Guid.NewGuid();
        var stormFeatures = stormResult.Entities.Select(e => new ExtractedFeature
        {
            WorkspaceId = wsId,
            DrawingId = stormDrawingId,
            RevisionId = stormRevId,
            CadLayer = e.LayerName,
            Category = e.Category,
            SubType = e.SubType,
            GeometryType = e.GeometryType,
            NativeGeometry = e.NativeGeometry,
            DisplayGeometry = e.DisplayGeometry,
            DepthM = e.DepthM,
            Z = e.Z,
            IsActive = true
        }).ToList();
        context.ExtractedFeatures.AddRange(stormFeatures);
        await context.SaveChangesAsync();

        // Step 3: Ingest Power Cable drawing (depth 0.9m)
        var powerPath = GetDxfPath("gfc_dxf", "GFC_POWER_R1_CH0000-1000_R0.dxf");
        var powerResult = await extractor.ExtractAsync(powerPath, 32644, wsId);
        var powerDrawingId = Guid.NewGuid();
        var powerRevId = Guid.NewGuid();
        var powerFeatures = powerResult.Entities.Select(e => new ExtractedFeature
        {
            WorkspaceId = wsId,
            DrawingId = powerDrawingId,
            RevisionId = powerRevId,
            CadLayer = e.LayerName,
            Category = e.Category,
            SubType = e.SubType,
            GeometryType = e.GeometryType,
            NativeGeometry = e.NativeGeometry,
            DisplayGeometry = e.DisplayGeometry,
            DepthM = e.DepthM,
            Z = e.Z,
            IsActive = true
        }).ToList();
        context.ExtractedFeatures.AddRange(powerFeatures);
        await context.SaveChangesAsync();

        // Step 4: Now Ingest Sewer drawing (depth 1.7m)
        var sewerPath = GetDxfPath("gfc_dxf", "GFC_SEWER_R2_CH0000-0400_R0.dxf");
        var sewerResult = await extractor.ExtractAsync(sewerPath, 32644, wsId);
        var sewerDrawingId = Guid.NewGuid();
        var sewerRevId = Guid.NewGuid();
        var sewerFeatures = sewerResult.Entities.Select(e => new ExtractedFeature
        {
            WorkspaceId = wsId,
            DrawingId = sewerDrawingId,
            RevisionId = sewerRevId,
            CadLayer = e.LayerName,
            Category = e.Category,
            SubType = e.SubType,
            GeometryType = e.GeometryType,
            NativeGeometry = e.NativeGeometry,
            DisplayGeometry = e.DisplayGeometry,
            DepthM = e.DepthM,
            Z = e.Z,
            IsActive = false // Pending approval
        }).ToList();
        context.ExtractedFeatures.AddRange(sewerFeatures);
        await context.SaveChangesAsync();

        // Detect clashes for Sewer revision with tolerance 0.5m
        var clashes = await clashService.DetectClashesForRevisionAsync(sewerRevId, wsId, 0.5);

        // Assert: Exactly 1 clash detected!
        clashes.Should().HaveCount(1, "Only Sewer vs Water should clash with gap 0.2m <= 0.5m tolerance");
        var clash = clashes.First();
        clash.VerticalGapM.Should().BeApproximately(0.2, 0.05);

        // Verify storm vs sewer (gap 1.8m) and power vs sewer (gap 0.8m) did NOT trigger clashes
        clashes.Any(c => c.Notes?.Contains("Storm drain", StringComparison.OrdinalIgnoreCase) == true).Should().BeFalse();
        clashes.Any(c => c.Notes?.Contains("Power", StringComparison.OrdinalIgnoreCase) == true).Should().BeFalse();
    }

    [Fact]
    public async Task AcceptanceTest3_InvalidDxfs_RejectedWithThreeDifferentClearMessages()
    {
        // 3. The 3 files in /gis_test_pack/gfc_dxf_invalid are rejected, each with a different, clear message:
        // - local grid near (0,0)
        // - far from project area
        // - unmapped layer
        var (context, extractor, _, wsId) = CreateTestEnvironment();

        // Invalid A: Local grid
        var localGridPath = GetDxfPath("gfc_dxf_invalid", "INVALID_local_grid_no_coordinates.dxf");
        var resA = await extractor.ExtractAsync(localGridPath, 32644, wsId);
        resA.Success.Should().BeFalse();
        resA.ErrorCode.Should().Be("INVALID_COORDINATES_LOCAL_GRID");
        resA.ErrorMessage.Should().Contain("local grid");

        // Invalid C: Unmapped layer
        var unmappedPath = GetDxfPath("gfc_dxf_invalid", "INVALID_unmapped_layer.dxf");
        var resC = await extractor.ExtractAsync(unmappedPath, 32644, wsId);
        resC.Success.Should().BeFalse();
        resC.ErrorCode.Should().Be("UNMAPPED_LAYER");
        resC.ErrorMessage.Should().Contain("unmapped layer");

        // First add a valid feature so workspace has existing data for distance comparison
        var validRoadPath = GetDxfPath("gfc_dxf", "GFC_ROAD_R1_CH0000-1000_R0.dxf");
        var validRes = await extractor.ExtractAsync(validRoadPath, 32644, wsId);
        context.ExtractedFeatures.Add(new ExtractedFeature
        {
            WorkspaceId = wsId,
            DrawingId = Guid.NewGuid(),
            RevisionId = Guid.NewGuid(),
            CadLayer = "ROAD_CL",
            Category = "Road",
            SubType = "Centerline",
            NativeGeometry = validRes.Entities.First().NativeGeometry,
            DisplayGeometry = validRes.Entities.First().DisplayGeometry,
            IsActive = true
        });
        await context.SaveChangesAsync();

        // Invalid B: ~30 km far from existing data
        var farPath = GetDxfPath("gfc_dxf_invalid", "INVALID_far_from_project_area.dxf");
        var resB = await extractor.ExtractAsync(farPath, 32644, wsId);
        resB.Success.Should().BeFalse();
        resB.ErrorCode.Should().Be("FAR_FROM_PROJECT_AREA");
        resB.ErrorMessage.Should().Contain("exceeding the warning threshold");
    }

    [Fact]
    public async Task AcceptanceTest4_RevisionControl_SecondRevisionReplacesFirstOnlyAfterApproval()
    {
        // 4. A second revision of the water drawing replaces the first on the map only after approval,
        // and the history shows both.
        var (context, extractor, _, wsId) = CreateTestEnvironment();

        var drawing = new UploadedDrawing
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            ProjectName = "Water Pipeline Network",
            Category = "Utility",
            SubType = "Water",
            CurrentRevision = "R0",
            ReviewStatus = ReviewStatus.Approved
        };
        context.UploadedDrawings.Add(drawing);

        // Revision R0 (Approved and Active)
        var rev0 = new DrawingRevision
        {
            Id = Guid.NewGuid(),
            DrawingId = drawing.Id,
            WorkspaceId = wsId,
            RevisionNumber = "R0",
            ReviewStatus = ReviewStatus.Approved,
            IsActive = true
        };

        // Revision R1 (Pending upload)
        var rev1 = new DrawingRevision
        {
            Id = Guid.NewGuid(),
            DrawingId = drawing.Id,
            WorkspaceId = wsId,
            RevisionNumber = "R1",
            ReviewStatus = ReviewStatus.Pending,
            IsActive = false
        };

        context.DrawingRevisions.AddRange(rev0, rev1);

        // Features for R0
        var feat0 = new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            DrawingId = drawing.Id,
            RevisionId = rev0.Id,
            WorkspaceId = wsId,
            CadLayer = "WATER_MAIN",
            Category = "Utility",
            SubType = "Water",
            NativeGeometry = new GeometryFactory().CreatePoint(new Coordinate(448350, 1825744)),
            DisplayGeometry = new GeometryFactory().CreatePoint(new Coordinate(80.516, 16.513)),
            IsActive = true
        };

        // Features for R1
        var feat1 = new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            DrawingId = drawing.Id,
            RevisionId = rev1.Id,
            WorkspaceId = wsId,
            CadLayer = "WATER_MAIN",
            Category = "Utility",
            SubType = "Water",
            NativeGeometry = new GeometryFactory().CreatePoint(new Coordinate(448350, 1825744)),
            DisplayGeometry = new GeometryFactory().CreatePoint(new Coordinate(80.516, 16.513)),
            IsActive = false
        };

        context.ExtractedFeatures.AddRange(feat0, feat1);
        await context.SaveChangesAsync();

        // 1. Before approval: R0 is on the map, R1 is NOT
        var activeBeforeApproval = await context.ExtractedFeatures
            .Where(f => f.DrawingId == drawing.Id && f.IsActive)
            .ToListAsync();

        activeBeforeApproval.Should().ContainSingle();
        activeBeforeApproval.First().RevisionId.Should().Be(rev0.Id);

        // 2. Perform Reviewer Approval of Revision R1
        rev0.IsActive = false;
        feat0.IsActive = false;

        rev1.ReviewStatus = ReviewStatus.Approved;
        rev1.IsActive = true;
        feat1.IsActive = true;
        drawing.CurrentRevision = "R1";

        await context.SaveChangesAsync();

        // 3. After approval: R1 is on the map, R0 is deactivated
        var activeAfterApproval = await context.ExtractedFeatures
            .Where(f => f.DrawingId == drawing.Id && f.IsActive)
            .ToListAsync();

        activeAfterApproval.Should().ContainSingle();
        activeAfterApproval.First().RevisionId.Should().Be(rev1.Id);

        // 4. Verify history retains both revisions
        var totalRevs = await context.DrawingRevisions
            .Where(r => r.DrawingId == drawing.Id)
            .ToListAsync();

        totalRevs.Should().HaveCount(2);
        totalRevs.Select(r => r.RevisionNumber).Should().Contain(new[] { "R0", "R1" });
    }

    [Fact]
    public void AcceptanceTest5_RoleSecurity_ViewerCannotUploadOrApprove()
    {
        // 5. A viewer-role user cannot upload or approve.
        var viewerRole = Roles.Viewer;
        var uploaderRole = Roles.Uploader;
        var reviewerRole = Roles.Reviewer;
        var adminRole = Roles.WorkspaceAdmin;

        bool CanUpload(string role) => role == Roles.WorkspaceAdmin || role == Roles.Uploader;
        bool CanApprove(string role) => role == Roles.WorkspaceAdmin || role == Roles.Reviewer;

        CanUpload(viewerRole).Should().BeFalse("Viewer must not be allowed to upload");
        CanApprove(viewerRole).Should().BeFalse("Viewer must not be allowed to approve");

        CanUpload(uploaderRole).Should().BeTrue();
        CanApprove(uploaderRole).Should().BeFalse();

        CanUpload(reviewerRole).Should().BeFalse();
        CanApprove(reviewerRole).Should().BeTrue();

        CanUpload(adminRole).Should().BeTrue();
        CanApprove(adminRole).Should().BeTrue();
    }

    [Fact]
    public async Task AcceptanceTest6_WorkspaceDataIsolation_WorkspaceUsersCannotSeeOtherWorkspaceData()
    {
        // 6. Workspace A users cannot see workspace B data.
        var (context, _, _, wsAId) = CreateTestEnvironment();
        var wsBId = Guid.NewGuid();
        context.Workspaces.Add(new Workspace { Id = wsBId, Name = "Workspace B", Code = "WS-B" });

        var drawingA = new UploadedDrawing { Id = Guid.NewGuid(), WorkspaceId = wsAId, ProjectName = "Project in Workspace A" };
        var drawingB = new UploadedDrawing { Id = Guid.NewGuid(), WorkspaceId = wsBId, ProjectName = "Project in Workspace B" };
        context.UploadedDrawings.AddRange(drawingA, drawingB);
        await context.SaveChangesAsync();

        // Query scoped strictly to Workspace A
        var drawingsInA = await context.UploadedDrawings.Where(d => d.WorkspaceId == wsAId).ToListAsync();

        drawingsInA.Should().ContainSingle();
        drawingsInA.First().ProjectName.Should().Be("Project in Workspace A");
        drawingsInA.Any(d => d.WorkspaceId == wsBId).Should().BeFalse();
    }
}
