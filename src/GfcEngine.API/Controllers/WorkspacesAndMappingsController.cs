using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkspacesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WorkspacesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkspaceDto>>> GetAll()
    {
        var workspaces = await _context.Workspaces
            .Select(w => new WorkspaceDto
            {
                Id = w.Id,
                Name = w.Name,
                Code = w.Code,
                DefaultEpsg = w.DefaultEpsg,
                DisplayEpsg = w.DisplayEpsg,
                CenterLon = w.CenterLon,
                CenterLat = w.CenterLat,
                DefaultZoom = w.DefaultZoom,
                ClashVerticalToleranceM = w.ClashVerticalToleranceM,
                FarFromExistingDataWarningM = w.FarFromExistingDataWarningM,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();

        return Ok(workspaces);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkspaceDto>> GetById(Guid id)
    {
        var w = await _context.Workspaces.FindAsync(id);
        if (w == null) return NotFound();

        return Ok(new WorkspaceDto
        {
            Id = w.Id,
            Name = w.Name,
            Code = w.Code,
            DefaultEpsg = w.DefaultEpsg,
            DisplayEpsg = w.DisplayEpsg,
            CenterLon = w.CenterLon,
            CenterLat = w.CenterLat,
            DefaultZoom = w.DefaultZoom,
            ClashVerticalToleranceM = w.ClashVerticalToleranceM,
            FarFromExistingDataWarningM = w.FarFromExistingDataWarningM,
            CreatedAt = w.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> Create([FromBody] CreateWorkspaceDto dto)
    {
        if (await _context.Workspaces.AnyAsync(w => w.Code == dto.Code))
        {
            return BadRequest(new { message = $"Workspace code '{dto.Code}' already exists." });
        }

        var workspace = new Workspace
        {
            Name = dto.Name,
            Code = dto.Code,
            DefaultEpsg = dto.DefaultEpsg,
            DisplayEpsg = dto.DisplayEpsg,
            CenterLon = dto.CenterLon,
            CenterLat = dto.CenterLat,
            DefaultZoom = dto.DefaultZoom,
            ClashVerticalToleranceM = dto.ClashVerticalToleranceM,
            FarFromExistingDataWarningM = dto.FarFromExistingDataWarningM,
            CreatedAt = DateTime.UtcNow
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = workspace.Id }, new WorkspaceDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Code = workspace.Code,
            DefaultEpsg = workspace.DefaultEpsg,
            DisplayEpsg = workspace.DisplayEpsg,
            CenterLon = workspace.CenterLon,
            CenterLat = workspace.CenterLat,
            DefaultZoom = workspace.DefaultZoom,
            ClashVerticalToleranceM = workspace.ClashVerticalToleranceM,
            FarFromExistingDataWarningM = workspace.FarFromExistingDataWarningM,
            CreatedAt = workspace.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateWorkspaceDto dto)
    {
        var w = await _context.Workspaces.FindAsync(id);
        if (w == null) return NotFound();

        w.Name = dto.Name;
        w.DefaultEpsg = dto.DefaultEpsg;
        w.DisplayEpsg = dto.DisplayEpsg;
        w.CenterLon = dto.CenterLon;
        w.CenterLat = dto.CenterLat;
        w.DefaultZoom = dto.DefaultZoom;
        w.ClashVerticalToleranceM = dto.ClashVerticalToleranceM;
        w.FarFromExistingDataWarningM = dto.FarFromExistingDataWarningM;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/workspaces/{workspaceId}/mappings")]
[Authorize]
public class LayerMappingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LayerMappingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CadLayerMappingDto>>> GetMappings(Guid workspaceId)
    {
        var mappings = await _context.CadLayerMappings
            .Where(m => m.WorkspaceId == workspaceId)
            .OrderBy(m => m.Category)
            .ThenBy(m => m.CadLayer)
            .Select(m => new CadLayerMappingDto
            {
                Id = m.Id,
                WorkspaceId = m.WorkspaceId,
                CadLayer = m.CadLayer,
                Category = m.Category,
                SubType = m.SubType,
                GeometryType = m.GeometryType,
                Color = m.Color
            })
            .ToListAsync();

        return Ok(mappings);
    }

    [HttpPost]
    public async Task<ActionResult<CadLayerMappingDto>> CreateMapping(Guid workspaceId, [FromBody] CadLayerMappingDto dto)
    {
        var mapping = new CadLayerMapping
        {
            WorkspaceId = workspaceId,
            CadLayer = dto.CadLayer,
            Category = dto.Category,
            SubType = dto.SubType,
            GeometryType = dto.GeometryType,
            Color = dto.Color,
            CreatedAt = DateTime.UtcNow
        };

        _context.CadLayerMappings.Add(mapping);
        await _context.SaveChangesAsync();

        dto.Id = mapping.Id;
        dto.WorkspaceId = workspaceId;
        return CreatedAtAction(nameof(GetMappings), new { workspaceId }, dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMapping(Guid workspaceId, Guid id)
    {
        var mapping = await _context.CadLayerMappings.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.Id == id);
        if (mapping == null) return NotFound();

        _context.CadLayerMappings.Remove(mapping);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("import-csv")]
    public async Task<ActionResult<object>> ImportCsv(Guid workspaceId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No CSV file uploaded." });
        }

        var imported = new List<CadLayerMapping>();
        using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }))
        {
            var records = csv.GetRecords<dynamic>();
            foreach (var rec in records)
            {
                var dict = (IDictionary<string, object>)rec;
                var layer = dict["cad_layer"]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(layer)) continue;

                var existing = await _context.CadLayerMappings
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.CadLayer == layer);

                if (existing != null)
                {
                    existing.Category = dict["category"]?.ToString()?.Trim() ?? existing.Category;
                    existing.SubType = dict["sub_type"]?.ToString()?.Trim() ?? existing.SubType;
                    existing.GeometryType = dict["geometry"]?.ToString()?.Trim() ?? existing.GeometryType;
                    existing.Color = dict["color"]?.ToString()?.Trim() ?? existing.Color;
                }
                else
                {
                    imported.Add(new CadLayerMapping
                    {
                        WorkspaceId = workspaceId,
                        CadLayer = layer,
                        Category = dict["category"]?.ToString()?.Trim() ?? "Other",
                        SubType = dict["sub_type"]?.ToString()?.Trim() ?? "General",
                        GeometryType = dict["geometry"]?.ToString()?.Trim() ?? "line",
                        Color = dict["color"]?.ToString()?.Trim() ?? "#000000"
                    });
                }
            }
        }

        if (imported.Count > 0)
        {
            _context.CadLayerMappings.AddRange(imported);
        }
        await _context.SaveChangesAsync();

        return Ok(new { message = $"CSV imported successfully: {imported.Count} new mapping(s) added." });
    }
}
