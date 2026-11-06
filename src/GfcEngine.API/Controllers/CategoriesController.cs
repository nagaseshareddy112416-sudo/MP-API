using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryConfigDto>>> GetCategories(Guid workspaceId)
    {
        var list = await _context.CategoryConfigs
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryConfigDto
            {
                Id = c.Id,
                WorkspaceId = c.WorkspaceId,
                CategoryName = c.CategoryName,
                SubTypeName = c.SubTypeName,
                ColorHex = c.ColorHex,
                IconName = c.IconName,
                DisplayOrder = c.DisplayOrder
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{categoryName}/fields")]
    public async Task<ActionResult<IEnumerable<CategoryFieldConfigDto>>> GetCategoryFields(Guid workspaceId, string categoryName)
    {
        var fields = await _context.CategoryFieldConfigs
            .Where(f => f.WorkspaceId == workspaceId && f.CategoryName.ToLower() == categoryName.ToLower())
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new CategoryFieldConfigDto
            {
                Id = f.Id,
                WorkspaceId = f.WorkspaceId,
                CategoryName = f.CategoryName,
                FieldName = f.FieldName,
                FieldLabel = f.FieldLabel,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                OptionsJson = f.OptionsJson,
                DisplayOrder = f.DisplayOrder
            })
            .ToListAsync();

        return Ok(fields);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryConfigDto>> CreateCategory(Guid workspaceId, [FromBody] CategoryConfigDto dto)
    {
        var config = new CategoryConfig
        {
            WorkspaceId = workspaceId,
            CategoryName = dto.CategoryName,
            SubTypeName = dto.SubTypeName,
            ColorHex = dto.ColorHex,
            IconName = dto.IconName,
            DisplayOrder = dto.DisplayOrder
        };

        _context.CategoryConfigs.Add(config);
        await _context.SaveChangesAsync();

        dto.Id = config.Id;
        dto.WorkspaceId = workspaceId;
        return Ok(dto);
    }

    [HttpPost("{categoryName}/fields")]
    public async Task<ActionResult<CategoryFieldConfigDto>> CreateField(Guid workspaceId, string categoryName, [FromBody] CategoryFieldConfigDto dto)
    {
        var field = new CategoryFieldConfig
        {
            WorkspaceId = workspaceId,
            CategoryName = categoryName,
            FieldName = dto.FieldName,
            FieldLabel = dto.FieldLabel,
            FieldType = dto.FieldType,
            IsRequired = dto.IsRequired,
            OptionsJson = dto.OptionsJson,
            DisplayOrder = dto.DisplayOrder
        };

        _context.CategoryFieldConfigs.Add(field);
        await _context.SaveChangesAsync();

        dto.Id = field.Id;
        dto.WorkspaceId = workspaceId;
        dto.CategoryName = categoryName;
        return Ok(dto);
    }
}

