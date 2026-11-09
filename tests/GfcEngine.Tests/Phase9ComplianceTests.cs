using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;
using GfcEngine.Infrastructure.Services;
using Xunit;

namespace GfcEngine.Tests;

public class Phase9ComplianceTests
{
    private string GetDxfPath(string subFolder, string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var p = Path.Combine(baseDir, "..", "..", "..", "..", "..", "gis_test_pack", subFolder, fileName);
        if (File.Exists(p)) return Path.GetFullPath(p);

        p = Path.Combine(baseDir, "gis_test_pack", subFolder, fileName);
        if (File.Exists(p)) return Path.GetFullPath(p);

        p = Path.Combine("d:\\MP\\MP-API\\gis_test_pack", subFolder, fileName);
        if (File.Exists(p)) return Path.GetFullPath(p);

        throw new FileNotFoundException($"Cannot locate DXF: {subFolder}/{fileName}");
    }

    private (ApplicationDbContext context, NetDxfExtractionService extractionService, FeatureMatchingService matchService, ComplianceEngineService complianceService, Guid workspaceId) CreateTestEnvironment()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var transformer = new ProjNetCrsTransformer();
        var extractionService = new NetDxfExtractionService(context, transformer, NullLogger<NetDxfExtractionService>.Instance);
        var matchService = new FeatureMatchingService(context);
        var complianceService = new ComplianceEngineService(context, matchService);

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
            ClashVerticalToleranceM = 0.5
        };
        context.Workspaces.Add(ws);

        // CAD mappings
        context.CadLayerMappings.AddRange(
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "ROAD_CL", Category = "Road", SubType = "Centerline", GeometryType = "line", Color = "#000000" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "ROAD_EDGE", Category = "Road", SubType = "Edge of pavement", GeometryType = "line", Color = "#555555" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "CHAINAGE", Category = "Road", SubType = "Chainage marker", GeometryType = "point", Color = "#ff9800" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "WATER_MAIN", Category = "Utility", SubType = "Water", GeometryType = "line", Color = "#1f77ff" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "SEWER_LINE", Category = "Utility", SubType = "Sewer", GeometryType = "line", Color = "#8b4513" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "STORM_DRAIN", Category = "Utility", SubType = "Storm drain", GeometryType = "line", Color = "#2ca02c" },
            new CadLayerMapping { WorkspaceId = wsId, CadLayer = "POWER_CABLE", Category = "Utility", SubType = "Power", GeometryType = "line", Color = "#e63946" }
        );

        // Drawing Stages
        context.DrawingStages.AddRange(
            new DrawingStage { WorkspaceId = wsId, Name = "GFC", StageOrder = 5, IsGfcBaseline = true },
            new DrawingStage { WorkspaceId = wsId, Name = "As-Built", StageOrder = 7, IsAsBuilt = true }
        );

        // Tolerance Rules
        context.ToleranceRules.AddRange(
            new ToleranceRule { WorkspaceId = wsId, Category = "Road", SubType = "Centerline", HorizontalToleranceM = 0.10, VerticalToleranceM = 0.05, LengthToleranceM = 0.50 },
            new ToleranceRule { WorkspaceId = wsId, Category = "Road", SubType = "Edge of pavement", HorizontalToleranceM = 0.10, VerticalToleranceM = 0.05, LengthToleranceM = 0.50 },
            new ToleranceRule { WorkspaceId = wsId, Category = "Utility", SubType = "Water", HorizontalToleranceM = 0.20, VerticalToleranceM = 0.10, LengthToleranceM = 0.50 },
            new ToleranceRule { WorkspaceId = wsId, Category = "Utility", SubType = "Sewer", HorizontalToleranceM = 0.20, VerticalToleranceM = 0.10, LengthToleranceM = 0.50 },
            new ToleranceRule { WorkspaceId = wsId, Category = "Utility", SubType = "Storm drain", HorizontalToleranceM = 0.20, VerticalToleranceM = 0.10, LengthToleranceM = 0.50 },
            new ToleranceRule { WorkspaceId = wsId, Category = "Utility", SubType = "Power", HorizontalToleranceM = 0.30, VerticalToleranceM = 0.15, LengthToleranceM = 0.50 }
        );

        context.SaveChanges();
        return (context, extractionService, matchService, complianceService, wsId);
    }

    private async Task<(UploadedDrawing gfc, UploadedDrawing asBuilt)> IngestPairAsync(
        ApplicationDbContext context,
        NetDxfExtractionService extractionService,
        Guid wsId,
        string category,
        string subType,
        string gfcDxfName,
        string asBuiltDxfName,
        CoordinateSource source = CoordinateSource.Survey)
    {
        // 1. Ingest GFC
        var gfcDxf = GetDxfPath("gfc_dxf", gfcDxfName);
        var gfcRes = await extractionService.ExtractAsync(gfcDxf, 32644, wsId);
        gfcRes.Success.Should().BeTrue();

        var gfcDrawing = new UploadedDrawing
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            ProjectName = Path.GetFileNameWithoutExtension(gfcDxfName),
            Category = category,
            SubType = subType,
            CurrentRevision = "R0",
            ReviewStatus = ReviewStatus.Approved,
            SourceEpsg = 32644,
            StageName = "GFC"
        };
        context.UploadedDrawings.Add(gfcDrawing);

        var gfcRev = new DrawingRevision
        {
            Id = Guid.NewGuid(),
            DrawingId = gfcDrawing.Id,
            WorkspaceId = wsId,
            RevisionNumber = "R0",
            ReviewStatus = ReviewStatus.Approved,
            IsActive = true
        };
        context.DrawingRevisions.Add(gfcRev);

        foreach (var ent in gfcRes.Entities)
        {
            context.ExtractedFeatures.Add(new ExtractedFeature
            {
                WorkspaceId = wsId,
                DrawingId = gfcDrawing.Id,
                RevisionId = gfcRev.Id,
                CadLayer = ent.LayerName,
                Category = ent.Category,
                SubType = ent.SubType,
                GeometryType = ent.GeometryType,
                NativeGeometry = ent.NativeGeometry,
                DisplayGeometry = ent.DisplayGeometry,
                Z = ent.Z,
                DepthM = ent.DepthM,
                DiameterMm = ent.DiameterMm,
                Material = ent.Material,
                Chainage = ent.Chainage,
                IsActive = true
            });
        }

        // 2. Ingest As-Built
        var asBuiltDxf = GetDxfPath("asbuilt_dxf", asBuiltDxfName);
        var abRes = await extractionService.ExtractAsync(asBuiltDxf, 32644, wsId);
        abRes.Success.Should().BeTrue();

        var asBuiltDrawing = new UploadedDrawing
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            ProjectName = Path.GetFileNameWithoutExtension(asBuiltDxfName),
            Category = category,
            SubType = subType,
            CurrentRevision = "AB-01",
            ReviewStatus = ReviewStatus.Pending,
            SourceEpsg = 32644,
            StageName = "As-Built",
            GfcDrawingId = gfcDrawing.Id,
            SourceOfCoordinates = source
        };
        context.UploadedDrawings.Add(asBuiltDrawing);

        var abRev = new DrawingRevision
        {
            Id = Guid.NewGuid(),
            DrawingId = asBuiltDrawing.Id,
            WorkspaceId = wsId,
            RevisionNumber = "AB-01",
            ReviewStatus = ReviewStatus.Pending,
            IsActive = false
        };
        context.DrawingRevisions.Add(abRev);

        foreach (var ent in abRes.Entities)
        {
            context.ExtractedFeatures.Add(new ExtractedFeature
            {
                WorkspaceId = wsId,
                DrawingId = asBuiltDrawing.Id,
                RevisionId = abRev.Id,
                CadLayer = ent.LayerName,
                Category = ent.Category,
                SubType = ent.SubType,
                GeometryType = ent.GeometryType,
                NativeGeometry = ent.NativeGeometry,
                DisplayGeometry = ent.DisplayGeometry,
                Z = ent.Z,
                DepthM = ent.DepthM,
                DiameterMm = ent.DiameterMm,
                Material = ent.Material,
                Chainage = ent.Chainage,
                IsAsBuilt = true,
                IsActive = false
            });
        }

        await context.SaveChangesAsync();
        return (gfcDrawing, asBuiltDrawing);
    }

    [Fact]
    public async Task AcceptanceCriterion1_EvaluatesAllFiveComplianceCasesExpectedly()
    {
        var (context, extractionService, matchService, complianceService, wsId) = CreateTestEnvironment();

        // Case 1: Road R1 -> WithinTolerance (South edge offset 0.05m <= 0.10m tolerance)
        var (_, roadAb) = await IngestPairAsync(context, extractionService, wsId, "Road", "Centerline",
            "GFC_ROAD_R1_CH0000-1000_R0.dxf", "ASBUILT_ROAD_R1_CH0000-1000.dxf");

        var roadResults = await complianceService.EvaluateComplianceAsync(wsId, roadAb.Id);
        roadResults.Should().Contain(r => r.Status == ComplianceStatus.WithinTolerance);

        // Case 2: Water Main -> Deviation (shifted 0.6m > 0.20m, depth 1.9m vs 1.2m)
        var (_, waterAb) = await IngestPairAsync(context, extractionService, wsId, "Utility", "Water",
            "GFC_WATER_R1_CH0000-1000_R0.dxf", "ASBUILT_WATER_R1_CH0000-1000.dxf");

        var waterResults = await complianceService.EvaluateComplianceAsync(wsId, waterAb.Id);
        waterResults.Should().Contain(r => r.Status == ComplianceStatus.Deviation);
        var waterDev = waterResults.First(r => r.Status == ComplianceStatus.Deviation);
        waterDev.HorizontalDeviationM.Should().BeApproximately(0.60, 0.05);
        waterDev.VerticalDeviationM.Should().BeApproximately(0.40, 0.05); // 1.9m - 1.5m = 0.4m

        // Case 3: Sewer -> Compliant (identical)
        var (_, sewerAb) = await IngestPairAsync(context, extractionService, wsId, "Utility", "Sewer",
            "GFC_SEWER_R2_CH0000-0400_R0.dxf", "ASBUILT_SEWER_R2_CH0000-0400.dxf");

        var sewerResults = await complianceService.EvaluateComplianceAsync(wsId, sewerAb.Id);
        sewerResults.Should().Contain(r => r.Status == ComplianceStatus.Compliant);

        // Case 4: Storm drain omitted -> Missing
        // We link asBuilt sewer to storm drain GFC to check missing detection
        var stormDxf = GetDxfPath("gfc_dxf", "GFC_STORM_R1_CH0200-0800_R0.dxf");
        var stormRes = await extractionService.ExtractAsync(stormDxf, 32644, wsId);
        var stormGfc = new UploadedDrawing { Id = Guid.NewGuid(), WorkspaceId = wsId, ProjectName = "GFC_STORM", Category = "Utility", SubType = "Storm drain", SourceEpsg = 32644 };
        context.UploadedDrawings.Add(stormGfc);
        foreach (var ent in stormRes.Entities)
        {
            context.ExtractedFeatures.Add(new ExtractedFeature
            {
                WorkspaceId = wsId, DrawingId = stormGfc.Id, CadLayer = ent.LayerName, Category = ent.Category, SubType = ent.SubType,
                GeometryType = ent.GeometryType, NativeGeometry = ent.NativeGeometry, DisplayGeometry = ent.DisplayGeometry, IsActive = true
            });
        }
        // Empty as-built drawing for storm
        var emptyStormAb = new UploadedDrawing { Id = Guid.NewGuid(), WorkspaceId = wsId, ProjectName = "ASBUILT_STORM_OMITTED", Category = "Utility", SubType = "Storm drain", GfcDrawingId = stormGfc.Id, SourceEpsg = 32644 };
        context.UploadedDrawings.Add(emptyStormAb);
        await context.SaveChangesAsync();

        var stormResults = await complianceService.EvaluateComplianceAsync(wsId, emptyStormAb.Id);
        stormResults.Should().Contain(r => r.Status == ComplianceStatus.Missing);

        // Case 5: Power cable -> Extra branch
        var (_, powerAb) = await IngestPairAsync(context, extractionService, wsId, "Utility", "Power",
            "GFC_POWER_R1_CH0000-1000_R0.dxf", "ASBUILT_POWER_R1_CH0000-1000.dxf");

        var powerResults = await complianceService.EvaluateComplianceAsync(wsId, powerAb.Id);
        powerResults.Should().Contain(r => r.Status == ComplianceStatus.Extra);
    }

    [Fact]
    public async Task AcceptanceCriterion2_ComplianceReport_ListsResults_AndValidatesData()
    {
        var (context, extractionService, matchService, complianceService, wsId) = CreateTestEnvironment();

        var (_, waterAb) = await IngestPairAsync(context, extractionService, wsId, "Utility", "Water",
            "GFC_WATER_R1_CH0000-1000_R0.dxf", "ASBUILT_WATER_R1_CH0000-1000.dxf");

        var results = await complianceService.EvaluateComplianceAsync(wsId, waterAb.Id);
        results.Should().NotBeEmpty();

        // Verify result contains deviations with valid metrics
        var dev = results.FirstOrDefault(r => r.Status == ComplianceStatus.Deviation);
        dev.Should().NotBeNull();
        dev!.HorizontalDeviationM.Should().BeGreaterThan(0.2);
        dev.VerticalDeviationM.Should().BeGreaterThan(0.1);
    }

    [Fact]
    public async Task AcceptanceCriterion3_AcceptingDeviation_UpdatesStatus_AndAuditLogs()
    {
        var (context, extractionService, matchService, complianceService, wsId) = CreateTestEnvironment();

        var (_, waterAb) = await IngestPairAsync(context, extractionService, wsId, "Utility", "Water",
            "GFC_WATER_R1_CH0000-1000_R0.dxf", "ASBUILT_WATER_R1_CH0000-1000.dxf");

        var results = await complianceService.EvaluateComplianceAsync(wsId, waterAb.Id);
        var devResult = results.First(r => r.Status == ComplianceStatus.Deviation);
        var deviation = devResult.Deviations.First();

        var auditMock = new AuditService(context);
        var controller = new API.Controllers.DeviationsController(context, auditMock);

        var dto = new ReviewDeviationDto
        {
            Decision = DeviationDecision.Accepted,
            Reason = "Approved by Lead Civil Engineer due to site rock outcrop obstacle.",
            Comment = "Pipe lowered to 1.9m for adequate ground cover."
        };

        var actionResult = await controller.ReviewDeviation(wsId, deviation.Id, dto);
        actionResult.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();

        // Verify DB update
        var updatedDev = await context.Deviations.FindAsync(deviation.Id);
        updatedDev!.Decision.Should().Be(DeviationDecision.Accepted);
        updatedDev.Reason.Should().Be(dto.Reason);

        // Verify Audit Log
        var logs = await context.AuditLogs.Where(a => a.WorkspaceId == wsId && a.Action == "DeviationDecision").ToListAsync();
        logs.Should().NotBeEmpty();
        logs.First().Details.Should().Contain("Accepted");
        logs.First().Details.Should().Contain(dto.Reason);
    }

    [Fact]
    public async Task AcceptanceCriterion4_ChangingToleranceRule_AndReevaluating_ChangesClassification()
    {
        var (context, extractionService, matchService, complianceService, wsId) = CreateTestEnvironment();

        var (_, waterAb) = await IngestPairAsync(context, extractionService, wsId, "Utility", "Water",
            "GFC_WATER_R1_CH0000-1000_R0.dxf", "ASBUILT_WATER_R1_CH0000-1000.dxf");

        // Initial evaluation: Deviation (water shifted 0.6m vs 0.20m rule)
        var resultsInitial = await complianceService.EvaluateComplianceAsync(wsId, waterAb.Id);
        resultsInitial.First(r => r.Category == "Utility").Status.Should().Be(ComplianceStatus.Deviation);

        // Update tolerance rule: Increase horizontal tolerance to 1.0m and vertical to 1.0m
        var rule = await context.ToleranceRules.FirstAsync(r => r.WorkspaceId == wsId && r.Category == "Utility" && r.SubType == "Water");
        rule.HorizontalToleranceM = 1.0;
        rule.VerticalToleranceM = 1.0;
        await context.SaveChangesAsync();

        // Re-evaluate: Now should be classified as WithinTolerance!
        var resultsReevaluated = await complianceService.EvaluateComplianceAsync(wsId, waterAb.Id);
        resultsReevaluated.First(r => r.Category == "Utility").Status.Should().Be(ComplianceStatus.WithinTolerance);
    }

    [Fact]
    public async Task AcceptanceCriterion5_AsBuiltMarkedRedrawn_ShowsWarningInReport()
    {
        var (context, extractionService, matchService, complianceService, wsId) = CreateTestEnvironment();

        // Ingest drawing with CoordinateSource = Redrawn
        var (_, roadAb) = await IngestPairAsync(context, extractionService, wsId, "Road", "Centerline",
            "GFC_ROAD_R1_CH0000-1000_R0.dxf", "ASBUILT_ROAD_R1_CH0000-1000.dxf", CoordinateSource.Redrawn);

        roadAb.SourceOfCoordinates.Should().Be(CoordinateSource.Redrawn);

        await complianceService.EvaluateComplianceAsync(wsId, roadAb.Id);

        var auditMock = new AuditService(context);
        var controller = new API.Controllers.ComplianceController(context, complianceService, auditMock);

        var res = await controller.GetResults(wsId, roadAb.Id);
        var okRes = res.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okRes.Should().NotBeNull();
        var dtos = okRes!.Value as List<ComplianceResultDto>;
        dtos.Should().NotBeNull();
        dtos!.All(d => d.HasNonSurveyWarning).Should().BeTrue();
        dtos!.First().SourceOfCoordinates.Should().Be(CoordinateSource.Redrawn);
    }
}
