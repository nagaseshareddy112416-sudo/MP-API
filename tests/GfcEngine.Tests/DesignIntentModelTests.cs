using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;
using GfcEngine.Infrastructure.Services;
using Xunit;

namespace GfcEngine.Tests;

public class DesignIntentModelTests
{
    private class MockFileStorage : IFileStorage
    {
        private readonly Dictionary<string, byte[]> _files = new();

        public Task<string> SaveFileAsync(Stream stream, string fileName, string subFolder, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var path = $"{subFolder}/{fileName}";
            _files[path] = ms.ToArray();
            return Task.FromResult(path);
        }

        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            if (_files.TryGetValue(relativePath, out var bytes))
            {
                return Task.FromResult<Stream>(new MemoryStream(bytes));
            }
            throw new FileNotFoundException();
        }

        public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            _files.Remove(relativePath);
            return Task.CompletedTask;
        }

        public string GetFullPath(string relativePath) => relativePath;
    }

    private (ApplicationDbContext context, DesignIntentService service, Guid workspaceId) CreateTestEnv()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var fileStorage = new MockFileStorage();
        var service = new DesignIntentService(context, fileStorage, NullLogger<DesignIntentService>.Instance);

        var wsId = Guid.NewGuid();
        context.Workspaces.Add(new Workspace
        {
            Id = wsId,
            Name = "Finished-Look Test Workspace",
            Code = "FINISH-WS",
            DefaultEpsg = 32644,
            DisplayEpsg = 4326,
            CenterLon = 80.516,
            CenterLat = 16.513
        });
        context.SaveChanges();

        return (context, service, wsId);
    }

    [Fact]
    public async Task Upload_ValidGlb_ParsesHeader_BoundingBox_AndMetadata()
    {
        var (context, service, wsId) = CreateTestEnv();
        var glbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "gis_test_pack", "house_test_pack", "TEST_house_model.glb");
        if (!File.Exists(glbPath))
        {
            glbPath = Path.Combine(Directory.GetCurrentDirectory(), "gis_test_pack", "house_test_pack", "TEST_house_model.glb");
        }
        if (!File.Exists(glbPath))
        {
            glbPath = @"d:\MP\MP-API\gis_test_pack\house_test_pack\TEST_house_model.glb";
        }

        using var stream = File.OpenRead(glbPath);
        var result = await service.ProcessAndSaveModelAsync(wsId, null, "TEST_house_model.glb", stream, "test_engineer");

        result.Should().NotBeNull();
        result.FileType.Should().Be("GLB");
        result.WidthM.Should().BeApproximately(10.0, 0.5);
        result.DepthM.Should().BeApproximately(8.0, 0.5);
        result.HeightM.Should().BeApproximately(8.5, 0.5);
        result.DetectedUnit.Should().Be("m");
        result.ValidationStatus.Should().Be("Valid");
    }

    [Fact]
    public async Task ProprietaryFormats_SkpAndRvt_AreRejectedWithExplicitGuidance()
    {
        var (context, service, wsId) = CreateTestEnv();
        using var dummyStream = new MemoryStream(new byte[] { 0, 1, 2, 3 });

        // Attempt .rvt
        var rvtAct = async () => await service.ProcessAndSaveModelAsync(wsId, null, "villa.rvt", dummyStream, "architect");
        await rvtAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Revit*IFC*glTF*");

        // Attempt .skp
        dummyStream.Position = 0;
        var skpAct = async () => await service.ProcessAndSaveModelAsync(wsId, null, "house.skp", dummyStream, "architect");
        await skpAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SketchUp*glTF*");
    }

    [Fact]
    public async Task UnitSanityCheck_DetectsMillimeters_AndAppliesAutoCorrection()
    {
        var (context, service, wsId) = CreateTestEnv();

        // Create a model with mm dimensions (e.g. 10000 x 8000 x 6000 mm)
        var model = new DesignModel
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            ModelName = "Oversized_Millimeter_Building",
            OriginalFileName = "oversized.glb",
            StoragePath = "models/original/oversized.glb",
            FileType = "GLB",
            WidthM = 10000.0,
            DepthM = 8000.0,
            HeightM = 6000.0,
            Scale = 1.0,
            DetectedUnit = "mm"
        };
        context.DesignModels.Add(model);
        await context.SaveChangesAsync();

        var converted = await service.ConvertModelUnitsAsync(wsId, model.Id, "mm", "m");

        converted.Scale.Should().BeApproximately(0.001, 0.00001);
        converted.DetectedUnit.Should().Be("m");
        converted.UnitCorrectionApplied.Should().BeTrue();
    }

    [Fact]
    public async Task SnapToFootprint_ComputesCentroid_Orientation_AndDimensions()
    {
        var (context, service, wsId) = CreateTestEnv();
        var drawingId = Guid.NewGuid();

        // Create building footprint feature: 10m x 8m polygon at Amaravati coordinates
        var gf = new GeometryFactory(new PrecisionModel(), 4326);
        double minLon = 80.5150;
        double minLat = 16.5125;
        // ~10m in longitude (~0.000093 deg) and ~8m in latitude (~0.000072 deg)
        double maxLon = minLon + 0.0000936;
        double maxLat = minLat + 0.0000723;

        var poly = gf.CreatePolygon(new[]
        {
            new Coordinate(minLon, minLat),
            new Coordinate(maxLon, minLat),
            new Coordinate(maxLon, maxLat),
            new Coordinate(minLon, maxLat),
            new Coordinate(minLon, minLat)
        });

        var feature = new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = drawingId,
            CadLayer = "BLDG_FOOTPRINT",
            Category = "Building",
            SubType = "Footprint",
            GeometryType = "polygon",
            NativeGeometry = poly,
            DisplayGeometry = poly,
            IsActive = true
        };
        context.ExtractedFeatures.Add(feature);

        var model = new DesignModel
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = drawingId,
            ModelName = "Test House",
            OriginalFileName = "test_house.glb",
            StoragePath = "models/test.glb",
            FileType = "GLB",
            WidthM = 10.0,
            DepthM = 8.0,
            HeightM = 8.5,
            Scale = 1.0,
            DetectedUnit = "m"
        };
        context.DesignModels.Add(model);
        await context.SaveChangesAsync();

        var snap = await service.SnapToFootprintAsync(wsId, model.Id, drawingId);

        snap.Success.Should().BeTrue();
        snap.CenterLon.Should().BeApproximately((minLon + maxLon) / 2.0, 0.00001);
        snap.CenterLat.Should().BeApproximately((minLat + maxLat) / 2.0, 0.00001);
        snap.FootprintWidthM.Should().BeApproximately(10.0, 1.0);
        snap.FootprintDepthM.Should().BeApproximately(8.0, 1.0);
        snap.UnitMismatchDetected.Should().BeFalse();
    }

    [Fact]
    public async Task PlantLibrary_MapsCadBlocksToLicensed3DAssets()
    {
        var (context, service, wsId) = CreateTestEnv();

        var library = await service.GetPlantLibraryAsync(wsId);

        library.Should().NotBeEmpty();
        library.Should().Contain(p => p.CadBlockName == "TREE" && p.LicenseName.Contains("CC0"));
        library.Should().Contain(p => p.CadBlockName == "PALM" && p.LicenseName.Contains("CC-BY"));
        library.Should().Contain(p => p.CadBlockName == "SHRUB");
    }

    [Fact]
    public async Task StageViews_ReturnsDesignUnderConstructionAndAsBuiltData()
    {
        var (context, service, wsId) = CreateTestEnv();
        var drawingId = Guid.NewGuid();

        // 1. Add design model
        context.DesignModels.Add(new DesignModel
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = drawingId,
            ModelName = "Design Villa",
            OriginalFileName = "villa.glb",
            StoragePath = "models/villa.glb",
            FileType = "GLB",
            WidthM = 10.0,
            DepthM = 8.0,
            HeightM = 6.0
        });

        // 2. Add an under-construction feature
        var gf = new GeometryFactory(new PrecisionModel(), 4326);
        var pt = gf.CreatePoint(new Coordinate(80.516, 16.513));
        context.ExtractedFeatures.Add(new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = drawingId,
            CadLayer = "WATER_MAIN",
            Category = "Utility",
            SubType = "Water",
            GeometryType = "line",
            NativeGeometry = pt,
            DisplayGeometry = pt,
            ConstructionStatus = ConstructionStatus.UnderConstruction,
            IsActive = true
        });

        // 3. Add an as-built reality feature
        context.ExtractedFeatures.Add(new ExtractedFeature
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            DrawingId = drawingId,
            CadLayer = "WATER_MAIN",
            Category = "Utility",
            SubType = "Water",
            GeometryType = "line",
            NativeGeometry = pt,
            DisplayGeometry = pt,
            IsAsBuilt = true,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var stageData = await service.GetStageViewsDataAsync(wsId);

        stageData.DesignModels.Should().HaveCount(1);
        stageData.ConstructionComponents.Should().HaveCount(1);
        stageData.ConstructionComponents.First().ConstructionStatus.Should().Be("UnderConstruction");
        stageData.ConstructionComponents.First().ProgressPercent.Should().Be(45.0);
        stageData.AsBuiltFeatures.Should().HaveCount(1);
    }
}
