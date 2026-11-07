using System.Globalization;
using System.Security.Claims;
using System.Text;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/compliance")]
[Authorize]
public class ComplianceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IComplianceEngineService _complianceService;
    private readonly IAuditService _auditService;

    public ComplianceController(
        ApplicationDbContext context,
        IComplianceEngineService complianceService,
        IAuditService auditService)
    {
        _context = context;
        _complianceService = complianceService;
        _auditService = auditService;
    }

    [HttpPost("evaluate/{asBuiltDrawingId}")]
    public async Task<ActionResult<List<ComplianceResultDto>>> Evaluate(Guid workspaceId, Guid asBuiltDrawingId)
    {
        var results = await _complianceService.EvaluateComplianceAsync(workspaceId, asBuiltDrawingId);

        var asBuiltDrawing = await _context.UploadedDrawings.FindAsync(asBuiltDrawingId);
        bool hasNonSurveyWarning = asBuiltDrawing?.SourceOfCoordinates == CoordinateSource.Redrawn ||
                                  asBuiltDrawing?.SourceOfCoordinates == CoordinateSource.Unknown;

        var dtos = results.Select(r => new ComplianceResultDto
        {
            Id = r.Id,
            WorkspaceId = r.WorkspaceId,
            AsBuiltDrawingId = r.AsBuiltDrawingId,
            GfcDrawingId = r.GfcDrawingId,
            GfcRevisionId = r.GfcRevisionId,
            AsBuiltFeatureId = r.AsBuiltFeatureId,
            GfcFeatureId = r.GfcFeatureId,
            Category = r.Category,
            SubType = r.SubType,
            ChainageStart = r.ChainageStart,
            ChainageEnd = r.ChainageEnd,
            HorizontalDeviationM = r.HorizontalDeviationM,
            AverageHorizontalDeviationM = r.AverageHorizontalDeviationM,
            VerticalDeviationM = r.VerticalDeviationM,
            LengthDeviationM = r.LengthDeviationM,
            AttributeDiffsJson = r.AttributeDiffsJson,
            Status = r.Status,
            ComputedAt = r.ComputedAt,
            SourceOfCoordinates = asBuiltDrawing?.SourceOfCoordinates,
            HasNonSurveyWarning = hasNonSurveyWarning,
            Deviations = r.Deviations.Select(d => new DeviationDto
            {
                Id = d.Id,
                ComplianceResultId = d.ComplianceResultId,
                Decision = d.Decision,
                Reason = d.Reason,
                DecidedBy = d.DecidedBy,
                DecidedAt = d.DecidedAt,
                Comment = d.Comment
            }).ToList()
        }).ToList();

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.Identity?.Name ?? "system";
        await _auditService.LogAsync(workspaceId, "ComplianceEvaluated", "UploadedDrawing", asBuiltDrawingId.ToString(),
            $"Evaluated compliance for as-built drawing: {results.Count} items classified.", userId);

        return Ok(dtos);
    }

    [HttpGet("results/{asBuiltDrawingId}")]
    public async Task<ActionResult<List<ComplianceResultDto>>> GetResults(Guid workspaceId, Guid asBuiltDrawingId)
    {
        var asBuiltDrawing = await _context.UploadedDrawings.FindAsync(asBuiltDrawingId);
        bool hasNonSurveyWarning = asBuiltDrawing?.SourceOfCoordinates == CoordinateSource.Redrawn ||
                                  asBuiltDrawing?.SourceOfCoordinates == CoordinateSource.Unknown;

        var results = await _context.ComplianceResults
            .Where(r => r.WorkspaceId == workspaceId && r.AsBuiltDrawingId == asBuiltDrawingId)
            .Include(r => r.Deviations)
            .Include(r => r.AsBuiltDrawing)
            .Include(r => r.GfcDrawing)
            .Include(r => r.GfcRevision)
            .ToListAsync();

        var dtos = results.Select(r => new ComplianceResultDto
        {
            Id = r.Id,
            WorkspaceId = r.WorkspaceId,
            AsBuiltDrawingId = r.AsBuiltDrawingId,
            AsBuiltDrawingName = r.AsBuiltDrawing?.ProjectName ?? "",
            GfcDrawingId = r.GfcDrawingId,
            GfcDrawingName = r.GfcDrawing?.ProjectName ?? "",
            GfcRevisionId = r.GfcRevisionId,
            GfcRevisionNumber = r.GfcRevision?.RevisionNumber ?? "",
            AsBuiltFeatureId = r.AsBuiltFeatureId,
            GfcFeatureId = r.GfcFeatureId,
            Category = r.Category,
            SubType = r.SubType,
            ChainageStart = r.ChainageStart,
            ChainageEnd = r.ChainageEnd,
            HorizontalDeviationM = r.HorizontalDeviationM,
            AverageHorizontalDeviationM = r.AverageHorizontalDeviationM,
            VerticalDeviationM = r.VerticalDeviationM,
            LengthDeviationM = r.LengthDeviationM,
            AttributeDiffsJson = r.AttributeDiffsJson,
            Status = r.Status,
            ComputedAt = r.ComputedAt,
            SourceOfCoordinates = asBuiltDrawing?.SourceOfCoordinates,
            HasNonSurveyWarning = hasNonSurveyWarning,
            Deviations = r.Deviations.Select(d => new DeviationDto
            {
                Id = d.Id,
                ComplianceResultId = d.ComplianceResultId,
                Decision = d.Decision,
                Reason = d.Reason,
                DecidedBy = d.DecidedBy,
                DecidedAt = d.DecidedAt,
                Comment = d.Comment
            }).ToList()
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ComplianceSummaryDto>> GetSummary(Guid workspaceId)
    {
        var results = await _context.ComplianceResults
            .Where(r => r.WorkspaceId == workspaceId)
            .Include(r => r.Deviations)
            .ToListAsync();

        var summary = new ComplianceSummaryDto
        {
            WorkspaceId = workspaceId,
            TotalFeatures = results.Count,
            CompliantCount = results.Count(r => r.Status == ComplianceStatus.Compliant),
            WithinToleranceCount = results.Count(r => r.Status == ComplianceStatus.WithinTolerance),
            DeviationCount = results.Count(r => r.Status == ComplianceStatus.Deviation),
            MissingCount = results.Count(r => r.Status == ComplianceStatus.Missing),
            ExtraCount = results.Count(r => r.Status == ComplianceStatus.Extra)
        };

        int totalPassing = summary.CompliantCount + summary.WithinToleranceCount;
        summary.CompliancePercentage = summary.TotalFeatures > 0
            ? Math.Round((double)totalPassing / summary.TotalFeatures * 100.0, 1)
            : 0;

        var allDeviations = results.SelectMany(r => r.Deviations).ToList();
        summary.OpenDeviationsCount = allDeviations.Count(d => d.Decision == DeviationDecision.Pending || d.Decision == DeviationDecision.Rectify);
        summary.AcceptedDeviationsCount = allDeviations.Count(d => d.Decision == DeviationDecision.Accepted);

        // Group by category
        var catGroups = results.GroupBy(r => r.Category);
        foreach (var grp in catGroups)
        {
            int total = grp.Count();
            int compliant = grp.Count(r => r.Status == ComplianceStatus.Compliant);
            int withinTol = grp.Count(r => r.Status == ComplianceStatus.WithinTolerance);
            summary.ByCategory[grp.Key] = new CategoryComplianceDto
            {
                Category = grp.Key,
                Total = total,
                Compliant = compliant,
                WithinTolerance = withinTol,
                Deviations = grp.Count(r => r.Status == ComplianceStatus.Deviation),
                Missing = grp.Count(r => r.Status == ComplianceStatus.Missing),
                Extra = grp.Count(r => r.Status == ComplianceStatus.Extra),
                CompliancePercentage = total > 0 ? Math.Round((double)(compliant + withinTol) / total * 100.0, 1) : 0
            };
        }

        return Ok(summary);
    }

    [HttpGet("rules")]
    public async Task<ActionResult<IEnumerable<ToleranceRuleDto>>> GetRules(Guid workspaceId)
    {
        var rules = await _context.ToleranceRules
            .Where(r => r.WorkspaceId == workspaceId)
            .OrderBy(r => r.Category)
            .ThenBy(r => r.SubType)
            .Select(r => new ToleranceRuleDto
            {
                Id = r.Id,
                WorkspaceId = r.WorkspaceId,
                Category = r.Category,
                SubType = r.SubType,
                HorizontalToleranceM = r.HorizontalToleranceM,
                VerticalToleranceM = r.VerticalToleranceM,
                LengthToleranceM = r.LengthToleranceM,
                AttributeRulesJson = r.AttributeRulesJson,
                Notes = r.Notes
            })
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPut("rules/{id}")]
    public async Task<ActionResult> UpdateRule(Guid workspaceId, Guid id, [FromBody] ToleranceRuleDto dto)
    {
        var rule = await _context.ToleranceRules.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == id);
        if (rule == null) return NotFound();

        rule.HorizontalToleranceM = dto.HorizontalToleranceM;
        rule.VerticalToleranceM = dto.VerticalToleranceM;
        rule.LengthToleranceM = dto.LengthToleranceM;
        rule.AttributeRulesJson = dto.AttributeRulesJson;
        rule.Notes = dto.Notes;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("export/csv/{asBuiltDrawingId}")]
    public async Task<IActionResult> ExportCsv(Guid workspaceId, Guid asBuiltDrawingId)
    {
        var asBuiltDrawing = await _context.UploadedDrawings.FindAsync(asBuiltDrawingId);
        var coordSourceWarning = (asBuiltDrawing?.SourceOfCoordinates == CoordinateSource.Redrawn ||
                                  asBuiltDrawing?.SourceOfCoordinates == CoordinateSource.Unknown)
            ? $"WARNING: Coordinate source is {asBuiltDrawing?.SourceOfCoordinates} (Non-Survey)"
            : "";

        var results = await _context.ComplianceResults
            .Where(r => r.WorkspaceId == workspaceId && r.AsBuiltDrawingId == asBuiltDrawingId)
            .Include(r => r.Deviations)
            .ToListAsync();

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            var records = results.Select(r =>
            {
                var latestDev = r.Deviations.OrderByDescending(d => d.DecidedAt ?? DateTime.MinValue).FirstOrDefault();
                return new
                {
                    r.Category,
                    r.SubType,
                    Chainage = r.ChainageStart?.ToString("F2") ?? "N/A",
                    Status = r.Status.ToString(),
                    HorizontalDev_M = r.HorizontalDeviationM.ToString("F3"),
                    VerticalDev_M = r.VerticalDeviationM.ToString("F3"),
                    LengthDev_M = r.LengthDeviationM.ToString("F3"),
                    Decision = latestDev?.Decision.ToString() ?? "N/A",
                    DecisionReason = latestDev?.Reason ?? "",
                    DecidedBy = latestDev?.DecidedBy ?? "",
                    CoordinateSource = asBuiltDrawing?.SourceOfCoordinates.ToString() ?? "Unknown",
                    CoordinateWarning = coordSourceWarning
                };
            });

            csv.WriteRecords(records);
        }

        var fileName = $"ComplianceReport_{asBuiltDrawing?.ProjectName ?? asBuiltDrawingId.ToString()}_{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(memoryStream.ToArray(), "text/csv", fileName);
    }

    [HttpGet("export/geojson/{asBuiltDrawingId}")]
    public async Task<IActionResult> ExportGeoJson(Guid workspaceId, Guid asBuiltDrawingId)
    {
        var results = await _context.ComplianceResults
            .Where(r => r.WorkspaceId == workspaceId && r.AsBuiltDrawingId == asBuiltDrawingId)
            .Include(r => r.AsBuiltFeature)
            .Include(r => r.GfcFeature)
            .Include(r => r.Deviations)
            .ToListAsync();

        var geoJsonWriter = new GeoJsonWriter();
        var featureList = new List<object>();

        foreach (var r in results)
        {
            var geom = r.AsBuiltFeature?.DisplayGeometry ?? r.GfcFeature?.DisplayGeometry;
            if (geom == null) continue;

            var latestDev = r.Deviations.FirstOrDefault();
            var props = new Dictionary<string, object>
            {
                ["complianceId"] = r.Id,
                ["category"] = r.Category,
                ["subType"] = r.SubType,
                ["status"] = r.Status.ToString(),
                ["horizontalDeviationM"] = r.HorizontalDeviationM,
                ["verticalDeviationM"] = r.VerticalDeviationM,
                ["lengthDeviationM"] = r.LengthDeviationM,
                ["decision"] = latestDev?.Decision.ToString() ?? "None",
                ["decisionReason"] = latestDev?.Reason ?? ""
            };

            featureList.Add(new
            {
                type = "Feature",
                geometry = System.Text.Json.JsonSerializer.Deserialize<object>(geoJsonWriter.Write(geom)),
                properties = props
            });
        }

        var featureCollection = new
        {
            type = "FeatureCollection",
            features = featureList
        };

        return Ok(featureCollection);
    }
}
