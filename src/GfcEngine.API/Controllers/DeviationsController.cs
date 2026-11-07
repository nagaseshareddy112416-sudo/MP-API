using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/deviations")]
[Authorize]
public class DeviationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DeviationsController(
        ApplicationDbContext context,
        IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ComplianceResultDto>>> GetDeviations(
        Guid workspaceId,
        [FromQuery] string? category,
        [FromQuery] ComplianceStatus? status,
        [FromQuery] DeviationDecision? decision,
        [FromQuery] double? minChainage,
        [FromQuery] double? maxChainage)
    {
        var query = _context.ComplianceResults
            .Where(r => r.WorkspaceId == workspaceId)
            .Include(r => r.Deviations)
            .Include(r => r.AsBuiltDrawing)
            .Include(r => r.GfcDrawing)
            .Include(r => r.GfcRevision)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(r => r.Category == category);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (decision.HasValue)
        {
            query = query.Where(r => r.Deviations.Any(d => d.Decision == decision.Value));
        }

        if (minChainage.HasValue)
        {
            query = query.Where(r => r.ChainageStart >= minChainage.Value);
        }

        if (maxChainage.HasValue)
        {
            query = query.Where(r => r.ChainageStart <= maxChainage.Value);
        }

        var results = await query.OrderByDescending(r => r.ComputedAt).ToListAsync();

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
            SourceOfCoordinates = r.AsBuiltDrawing?.SourceOfCoordinates,
            HasNonSurveyWarning = r.AsBuiltDrawing?.SourceOfCoordinates == CoordinateSource.Redrawn ||
                                  r.AsBuiltDrawing?.SourceOfCoordinates == CoordinateSource.Unknown,
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

    [HttpPost("{deviationId}/decision")]
    public async Task<ActionResult> ReviewDeviation(
        Guid workspaceId,
        Guid deviationId,
        [FromBody] ReviewDeviationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new { message = "A mandatory reason must be provided when recording a deviation decision." });
        }

        var deviation = await _context.Deviations
            .Include(d => d.ComplianceResult)
            .FirstOrDefaultAsync(d => d.WorkspaceId == workspaceId && d.Id == deviationId);

        if (deviation == null)
            return NotFound(new { message = "Deviation item not found." });

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.Identity?.Name ?? "reviewer";

        deviation.Decision = dto.Decision;
        deviation.Reason = dto.Reason.Trim();
        deviation.Comment = dto.Comment?.Trim();
        deviation.DecidedBy = userId;
        deviation.DecidedAt = DateTime.UtcNow;

        // If accepted and associated feature was pending, update construction status or feature status
        if (dto.Decision == DeviationDecision.Accepted && deviation.ComplianceResult != null)
        {
            if (deviation.ComplianceResult.AsBuiltFeatureId.HasValue)
            {
                var feature = await _context.ExtractedFeatures.FindAsync(deviation.ComplianceResult.AsBuiltFeatureId.Value);
                if (feature != null)
                {
                    feature.ConstructionStatus = ConstructionStatus.Completed;
                }
            }
        }

        await _context.SaveChangesAsync();

        // Audit log decision
        await _auditService.LogAsync(
            workspaceId,
            "DeviationDecision",
            "Deviation",
            deviationId.ToString(),
            $"Deviation decided as {dto.Decision}. Reason: '{dto.Reason}'. Comment: '{dto.Comment}'",
            userId);

        return Ok(new DeviationDto
        {
            Id = deviation.Id,
            ComplianceResultId = deviation.ComplianceResultId,
            Decision = deviation.Decision,
            Reason = deviation.Reason,
            DecidedBy = deviation.DecidedBy,
            DecidedAt = deviation.DecidedAt,
            Comment = deviation.Comment
        });
    }
}
