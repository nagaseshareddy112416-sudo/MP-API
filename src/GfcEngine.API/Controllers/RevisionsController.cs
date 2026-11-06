using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/drawings/{drawingId}/revisions")]
[Authorize]
public class RevisionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IClashDetectionService _clashService;
    private readonly IAuditService _auditService;

    public RevisionsController(
        ApplicationDbContext context,
        IClashDetectionService clashService,
        IAuditService auditService)
    {
        _context = context;
        _clashService = clashService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RevisionDto>>> GetRevisions(Guid workspaceId, Guid drawingId)
    {
        var revisions = await _context.DrawingRevisions
            .Where(r => r.WorkspaceId == workspaceId && r.DrawingId == drawingId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RevisionDto
            {
                Id = r.Id,
                DrawingId = r.DrawingId,
                RevisionNumber = r.RevisionNumber,
                OriginalFileName = r.OriginalFileName,
                ReviewStatus = r.ReviewStatus,
                ReviewComments = r.ReviewComments,
                ReviewedBy = r.ReviewedBy,
                ReviewedAt = r.ReviewedAt,
                IsActive = r.IsActive,
                CreatedBy = r.CreatedBy,
                CreatedAt = r.CreatedAt,
                FeatureCount = r.Features.Count
            })
            .ToListAsync();

        return Ok(revisions);
    }

    [HttpPost("{revisionId}/review")]
    public async Task<IActionResult> ReviewRevision(Guid workspaceId, Guid drawingId, Guid revisionId, [FromBody] ReviewDecisionDto decision)
    {
        var reviewer = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Reviewer";
        var userRole = await _context.WorkspaceUserRoles
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

        // In MVP, verify user is Reviewer or WorkspaceAdmin
        if (userRole != null && userRole.Role == Roles.Viewer)
        {
            return Forbid();
        }

        var revision = await _context.DrawingRevisions
            .Include(r => r.Features)
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.DrawingId == drawingId && r.Id == revisionId);

        if (revision == null) return NotFound("Revision not found.");

        var drawing = await _context.UploadedDrawings.FindAsync(drawingId);
        if (drawing == null) return NotFound("Drawing not found.");

        var ws = await _context.Workspaces.FindAsync(workspaceId);
        double tolerance = ws?.ClashVerticalToleranceM ?? 0.5;

        revision.ReviewedBy = reviewer;
        revision.ReviewedAt = DateTime.UtcNow;
        revision.ReviewComments = decision.Comments;

        if (decision.Approved)
        {
            revision.ReviewStatus = ReviewStatus.Approved;
            revision.IsActive = true;
            drawing.ReviewStatus = ReviewStatus.Approved;
            drawing.Status = DrawingStatus.Approved;

            // Deactivate all previous revisions for this drawing
            var otherRevs = await _context.DrawingRevisions
                .Where(r => r.DrawingId == drawingId && r.Id != revisionId)
                .ToListAsync();

            foreach (var or in otherRevs)
            {
                or.IsActive = false;
            }

            // Deactivate previous features and activate new features
            var previousFeatures = await _context.ExtractedFeatures
                .Where(f => f.DrawingId == drawingId && f.RevisionId != revisionId)
                .ToListAsync();

            foreach (var pf in previousFeatures)
            {
                pf.IsActive = false;
            }

            foreach (var nf in revision.Features)
            {
                nf.IsActive = true;
            }

            await _context.SaveChangesAsync();

            // Run Clash Detection on the newly approved revision features
            await _clashService.DetectClashesForRevisionAsync(revisionId, workspaceId, tolerance);

            await _auditService.LogAsync(workspaceId, "Approve", "DrawingRevision", revision.Id.ToString(), $"Approved revision {revision.RevisionNumber} of drawing {drawing.ProjectName}", reviewer);
        }
        else
        {
            revision.ReviewStatus = ReviewStatus.Rejected;
            revision.IsActive = false;
            drawing.ReviewStatus = ReviewStatus.Rejected;

            foreach (var nf in revision.Features)
            {
                nf.IsActive = false;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(workspaceId, "Reject", "DrawingRevision", revision.Id.ToString(), $"Rejected revision {revision.RevisionNumber}: {decision.Comments}", reviewer);
        }

        return Ok(new { message = $"Revision {(decision.Approved ? "approved" : "rejected")} successfully." });
    }
}
