using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/clashes")]
[Authorize]
public class ClashesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClashesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClashDto>>> GetClashes(Guid workspaceId)
    {
        var clashes = await _context.Clashes
            .Where(c => c.WorkspaceId == workspaceId)
            .Include(c => c.FeatureA)
            .Include(c => c.FeatureB)
            .OrderByDescending(c => c.DetectedAt)
            .Select(c => new ClashDto
            {
                Id = c.Id,
                WorkspaceId = c.WorkspaceId,
                FeatureAId = c.FeatureAId,
                FeatureBId = c.FeatureBId,
                FeatureACategory = c.FeatureA != null ? c.FeatureA.Category : "",
                FeatureASubType = c.FeatureA != null ? c.FeatureA.SubType : "",
                FeatureADepth = c.FeatureA != null ? c.FeatureA.DepthM : null,
                FeatureBCategory = c.FeatureB != null ? c.FeatureB.Category : "",
                FeatureBSubType = c.FeatureB != null ? c.FeatureB.SubType : "",
                FeatureBDepth = c.FeatureB != null ? c.FeatureB.DepthM : null,
                Lon = c.DisplayLocation.Coordinate.X,
                Lat = c.DisplayLocation.Coordinate.Y,
                VerticalGapM = c.VerticalGapM,
                Status = c.Status,
                Notes = c.Notes,
                DetectedAt = c.DetectedAt
            })
            .ToListAsync();

        return Ok(clashes);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid workspaceId, Guid id, [FromBody] UpdateClashStatusDto dto)
    {
        var clash = await _context.Clashes.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Id == id);
        if (clash == null) return NotFound();

        clash.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.Notes))
        {
            clash.Notes = dto.Notes;
        }

        if (dto.Status == ClashStatus.Resolved)
        {
            clash.ResolvedAt = DateTime.UtcNow;
            clash.ResolvedBy = User.Identity?.Name ?? "User";
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Clash status updated." });
    }

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsv(Guid workspaceId)
    {
        var clashes = await _context.Clashes
            .Where(c => c.WorkspaceId == workspaceId)
            .Include(c => c.FeatureA)
            .Include(c => c.FeatureB)
            .ToListAsync();

        var records = clashes.Select(c => new
        {
            ClashId = c.Id,
            FeatureA = $"{c.FeatureA?.Category} - {c.FeatureA?.SubType} (Depth: {c.FeatureA?.DepthM}m)",
            FeatureB = $"{c.FeatureB?.Category} - {c.FeatureB?.SubType} (Depth: {c.FeatureB?.DepthM}m)",
            Lon = c.DisplayLocation.Coordinate.X,
            Lat = c.DisplayLocation.Coordinate.Y,
            VerticalGapMeters = c.VerticalGapM,
            Status = c.Status.ToString(),
            Notes = c.Notes,
            DetectedAt = c.DetectedAt.ToString("yyyy-MM-dd HH:mm:ss")
        });

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(records);
        }

        return File(memoryStream.ToArray(), "text/csv", $"workspace_{workspaceId}_clashes.csv");
    }
}

[ApiController]
[Route("api/workspaces/{workspaceId}/reference-layers")]
[Authorize]
public class ReferenceLayersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReferenceLayersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReferenceLayerDto>>> GetLayers(Guid workspaceId)
    {
        var list = await _context.ReferenceLayers
            .Where(r => r.WorkspaceId == workspaceId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReferenceLayerDto
            {
                Id = r.Id,
                WorkspaceId = r.WorkspaceId,
                Name = r.Name,
                Format = r.Format,
                Color = r.Color,
                IsVisible = r.IsVisible,
                GeoJsonData = r.GeoJsonData,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ReferenceLayerDto>> UploadLayer(Guid workspaceId, [FromForm] string name, [FromForm] string color, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File is required");

        string geoJsonData;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            geoJsonData = await reader.ReadToEndAsync();
        }

        var ext = Path.GetExtension(file.FileName).TrimStart('.').ToUpperInvariant();

        var layer = new ReferenceLayer
        {
            WorkspaceId = workspaceId,
            Name = string.IsNullOrEmpty(name) ? file.FileName : name,
            Format = ext,
            Color = string.IsNullOrEmpty(color) ? "#3f51b5" : color,
            IsVisible = true,
            GeoJsonData = geoJsonData,
            CreatedBy = User.Identity?.Name ?? "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferenceLayers.Add(layer);
        await _context.SaveChangesAsync();

        return Ok(new ReferenceLayerDto
        {
            Id = layer.Id,
            WorkspaceId = layer.WorkspaceId,
            Name = layer.Name,
            Format = layer.Format,
            Color = layer.Color,
            IsVisible = layer.IsVisible,
            GeoJsonData = layer.GeoJsonData,
            CreatedAt = layer.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLayer(Guid workspaceId, Guid id)
    {
        var layer = await _context.ReferenceLayers.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == id);
        if (layer == null) return NotFound();

        _context.ReferenceLayers.Remove(layer);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/workspaces/{workspaceId}/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboard(Guid workspaceId)
    {
        var drawings = await _context.UploadedDrawings
            .Where(d => d.WorkspaceId == workspaceId)
            .ToListAsync();

        var features = await _context.ExtractedFeatures
            .Where(f => f.WorkspaceId == workspaceId && f.IsActive)
            .ToListAsync();

        var clashes = await _context.Clashes
            .Where(c => c.WorkspaceId == workspaceId)
            .ToListAsync();

        var statusCounts = drawings
            .GroupBy(d => d.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var categoryCounts = features
            .GroupBy(f => f.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        // Road chainage progress
        var roads = drawings.Where(d => d.Category.Equals("Road", StringComparison.OrdinalIgnoreCase)).ToList();
        var roadProgressList = new List<RoadChainageProgressDto>();

        foreach (var road in roads)
        {
            double totalLen = (road.ChainageEnd.HasValue && road.ChainageStart.HasValue)
                ? Math.Max(0, road.ChainageEnd.Value - road.ChainageStart.Value)
                : 1000.0;

            double pct = road.Status switch
            {
                DrawingStatus.Completed => 100.0,
                DrawingStatus.UnderConstruction => 50.0,
                DrawingStatus.Approved => 25.0,
                _ => 10.0
            };

            roadProgressList.Add(new RoadChainageProgressDto
            {
                RoadName = road.ProjectName,
                TotalLengthM = totalLen,
                CompletedLengthM = totalLen * (pct / 100.0),
                PercentComplete = pct,
                Status = road.Status.ToString()
            });
        }

        var recentUploads = drawings
            .OrderByDescending(d => d.CreatedAt)
            .Take(5)
            .Select(d => new DrawingDto
            {
                Id = d.Id,
                ProjectName = d.ProjectName,
                Category = d.Category,
                SubType = d.SubType,
                CurrentRevision = d.CurrentRevision,
                Status = d.Status,
                ReviewStatus = d.ReviewStatus,
                CreatedAt = d.CreatedAt,
                ProcessingStatus = d.ProcessingStatus
            })
            .ToList();

        var stats = new DashboardStatsDto
        {
            TotalDrawings = drawings.Count,
            TotalFeatures = features.Count,
            TotalClashes = clashes.Count,
            OpenClashes = clashes.Count(c => c.Status == ClashStatus.Open),
            StatusCounts = statusCounts,
            CategoryCounts = categoryCounts,
            RoadProgress = roadProgressList,
            RecentUploads = recentUploads
        };

        return Ok(stats);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(Guid workspaceId)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.Timestamp)
            .Take(100)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Details = a.Details,
                UserId = a.UserId,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }
}
