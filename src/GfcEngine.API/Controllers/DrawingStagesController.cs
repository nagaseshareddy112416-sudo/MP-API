using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/stages")]
[Authorize]
public class DrawingStagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DrawingStagesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DrawingStageDto>>> GetStages(Guid workspaceId)
    {
        var stages = await _context.DrawingStages
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderBy(s => s.StageOrder)
            .Select(s => new DrawingStageDto
            {
                Id = s.Id,
                WorkspaceId = s.WorkspaceId,
                Name = s.Name,
                StageOrder = s.StageOrder,
                IsDefault = s.IsDefault,
                IsGfcBaseline = s.IsGfcBaseline,
                IsAsBuilt = s.IsAsBuilt,
                Description = s.Description
            })
            .ToListAsync();

        return Ok(stages);
    }

    [HttpPost]
    public async Task<ActionResult<DrawingStageDto>> CreateStage(Guid workspaceId, [FromBody] CreateDrawingStageDto dto)
    {
        var stage = new DrawingStage
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = dto.Name,
            StageOrder = dto.StageOrder,
            IsDefault = dto.IsDefault,
            IsGfcBaseline = dto.IsGfcBaseline,
            IsAsBuilt = dto.IsAsBuilt,
            Description = dto.Description
        };

        _context.DrawingStages.Add(stage);
        await _context.SaveChangesAsync();

        return Ok(new DrawingStageDto
        {
            Id = stage.Id,
            WorkspaceId = stage.WorkspaceId,
            Name = stage.Name,
            StageOrder = stage.StageOrder,
            IsDefault = stage.IsDefault,
            IsGfcBaseline = stage.IsGfcBaseline,
            IsAsBuilt = stage.IsAsBuilt,
            Description = stage.Description
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateStage(Guid workspaceId, Guid id, [FromBody] CreateDrawingStageDto dto)
    {
        var stage = await _context.DrawingStages.FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Id == id);
        if (stage == null) return NotFound();

        stage.Name = dto.Name;
        stage.StageOrder = dto.StageOrder;
        stage.IsDefault = dto.IsDefault;
        stage.IsGfcBaseline = dto.IsGfcBaseline;
        stage.IsAsBuilt = dto.IsAsBuilt;
        stage.Description = dto.Description;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteStage(Guid workspaceId, Guid id)
    {
        var stage = await _context.DrawingStages.FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Id == id);
        if (stage == null) return NotFound();

        _context.DrawingStages.Remove(stage);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
