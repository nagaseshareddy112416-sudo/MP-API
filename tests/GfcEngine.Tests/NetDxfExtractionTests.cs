using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using GfcEngine.Domain.Entities;
using GfcEngine.Infrastructure.Persistence;
using GfcEngine.Infrastructure.Services;
using Xunit;

namespace GfcEngine.Tests;

public class NetDxfExtractionTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        // Seed test layer mappings
        var wsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        context.Workspaces.Add(new Workspace
        {
            Id = wsId,
            Name = "Test WS",
            Code = "TEST-01",
            DefaultEpsg = 32644,
            DisplayEpsg = 4326,
            CenterLon = 80.516,
            CenterLat = 16.513,
            FarFromExistingDataWarningM = 5000
        });

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
        return context;
    }

    private string GetTestDxfPath(string relativePath)
    {
        var testPack = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "gis_test_pack", relativePath);
        if (!File.Exists(testPack))
        {
            testPack = Path.Combine("d:\\MP\\MP-API\\gis_test_pack", relativePath);
        }
        if (!File.Exists(testPack))
        {
            testPack = Path.Combine("d:\\MP-API\\gis_test_pack", relativePath);
        }
        return Path.GetFullPath(testPack);
    }

    [Fact]
    public async Task Extract_ValidRoadDxf_ExtractsLinesAndChainageMarkers()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var transformer = new ProjNetCrsTransformer();
        var service = new NetDxfExtractionService(context, transformer, NullLogger<NetDxfExtractionService>.Instance);
        var wsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dxfPath = GetTestDxfPath("gfc_dxf/GFC_ROAD_R1_CH0000-1000_R0.dxf");

        // Act
        var result = await service.ExtractAsync(dxfPath, 32644, wsId);

        // Assert
        result.Success.Should().BeTrue();
        result.Entities.Should().NotBeEmpty();
        result.Entities.Any(e => e.LayerName == "ROAD_CL").Should().BeTrue();
        result.Entities.Any(e => e.LayerName == "CHAINAGE").Should().BeTrue();
    }

    [Fact]
    public async Task Extract_ValidWaterDxf_ExtractsNegativeZDepthAsPositiveDepthM()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var transformer = new ProjNetCrsTransformer();
        var service = new NetDxfExtractionService(context, transformer, NullLogger<NetDxfExtractionService>.Instance);
        var wsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dxfPath = GetTestDxfPath("gfc_dxf/GFC_WATER_R1_CH0000-1000_R0.dxf");

        // Act
        var result = await service.ExtractAsync(dxfPath, 32644, wsId);

        // Assert
        result.Success.Should().BeTrue();
        var water = result.Entities.FirstOrDefault(e => e.LayerName == "WATER_MAIN");
        water.Should().NotBeNull();
        water!.DepthM.Should().Be(1.5);
    }

    [Fact]
    public async Task Extract_InvalidLocalGridDxf_RejectsWithSpecificErrorCode()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var transformer = new ProjNetCrsTransformer();
        var service = new NetDxfExtractionService(context, transformer, NullLogger<NetDxfExtractionService>.Instance);
        var wsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dxfPath = GetTestDxfPath("gfc_dxf_invalid/INVALID_local_grid_no_coordinates.dxf");

        // Act
        var result = await service.ExtractAsync(dxfPath, 32644, wsId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_COORDINATES_LOCAL_GRID");
        result.ErrorMessage.Should().Contain("local grid");
    }

    [Fact]
    public async Task Extract_InvalidUnmappedLayerDxf_RejectsWithUnmappedLayerError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var transformer = new ProjNetCrsTransformer();
        var service = new NetDxfExtractionService(context, transformer, NullLogger<NetDxfExtractionService>.Instance);
        var wsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dxfPath = GetTestDxfPath("gfc_dxf_invalid/INVALID_unmapped_layer.dxf");

        // Act
        var result = await service.ExtractAsync(dxfPath, 32644, wsId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("UNMAPPED_LAYER");
        result.UnmappedLayers.Should().Contain("XYZ_UNKNOWN");
    }
}

