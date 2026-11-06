using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Jobs;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/drawings")]
[Authorize]
public class DrawingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly IDxfExtractionService _extractionService;
    private readonly ICrsTransformer _transformer;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IAuditService _auditService;
    private static readonly GeometryFactory _geomFactory = new(new PrecisionModel(), 32644);

    public DrawingsController(
        ApplicationDbContext context,
        IFileStorage fileStorage,
        IDxfExtractionService extractionService,
        ICrsTransformer transformer,
        IBackgroundJobClient backgroundJobClient,
        IAuditService auditService)
    {
        _context = context;
        _fileStorage = fileStorage;
        _extractionService = extractionService;
        _transformer = transformer;
        _backgroundJobClient = backgroundJobClient;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DrawingDto>>> GetDrawings(Guid workspaceId)
    {
        var drawings = await _context.UploadedDrawings
            .Where(d => d.WorkspaceId == workspaceId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DrawingDto
            {
                Id = d.Id,
                WorkspaceId = d.WorkspaceId,
                ProjectName = d.ProjectName,
                Category = d.Category,
                SubType = d.SubType,
                CurrentRevision = d.CurrentRevision,
                Status = d.Status,
                ReviewStatus = d.ReviewStatus,
                Contractor = d.Contractor,
                Consultant = d.Consultant,
                DrawingDate = d.DrawingDate,
                SourceEpsg = d.SourceEpsg,
                ChainageStart = d.ChainageStart,
                ChainageEnd = d.ChainageEnd,
                OriginalFileName = d.OriginalFileName,
                FileSize = d.FileSize,
                FileType = d.FileType,
                ProcessingStatus = d.ProcessingStatus,
                ProcessingError = d.ProcessingError,
                Notes = d.Notes,
                CreatedBy = d.CreatedBy,
                CreatedAt = d.CreatedAt,
                PinLon = d.DisplayPinLocation != null ? d.DisplayPinLocation.Coordinate.X : null,
                PinLat = d.DisplayPinLocation != null ? d.DisplayPinLocation.Coordinate.Y : null
            })
            .ToListAsync();

        return Ok(drawings);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DrawingDto>> GetById(Guid workspaceId, Guid id)
    {
        var d = await _context.UploadedDrawings.FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.Id == id);
        if (d == null) return NotFound();

        return Ok(new DrawingDto
        {
            Id = d.Id,
            WorkspaceId = d.WorkspaceId,
            ProjectName = d.ProjectName,
            Category = d.Category,
            SubType = d.SubType,
            CurrentRevision = d.CurrentRevision,
            Status = d.Status,
            ReviewStatus = d.ReviewStatus,
            Contractor = d.Contractor,
            Consultant = d.Consultant,
            DrawingDate = d.DrawingDate,
            SourceEpsg = d.SourceEpsg,
            ChainageStart = d.ChainageStart,
            ChainageEnd = d.ChainageEnd,
            OriginalFileName = d.OriginalFileName,
            FileSize = d.FileSize,
            FileType = d.FileType,
            ProcessingStatus = d.ProcessingStatus,
            ProcessingError = d.ProcessingError,
            Notes = d.Notes,
            CreatedBy = d.CreatedBy,
            CreatedAt = d.CreatedAt,
            PinLon = d.DisplayPinLocation != null ? d.DisplayPinLocation.Coordinate.X : null,
            PinLat = d.DisplayPinLocation != null ? d.DisplayPinLocation.Coordinate.Y : null
        });
    }

    [HttpPost("preview")]
    public async Task<ActionResult<object>> PreviewDrawing(Guid workspaceId, [FromForm] IFormFile file, [FromForm] int? sourceEpsg)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided." });
        }

        var epsg = sourceEpsg ?? 32644;
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}_{file.FileName}");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = await _extractionService.ExtractAsync(tempPath, epsg, workspaceId);
            if (!result.Success)
            {
                return BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage, unmappedLayers = result.UnmappedLayers });
            }

            var featuresGeoJson = result.Entities.Select(e => new
            {
                type = "Feature",
                geometry = System.Text.Json.JsonSerializer.Deserialize<object>(new NetTopologySuite.IO.GeoJsonWriter().Write(e.DisplayGeometry)),
                properties = new
                {
                    layer = e.LayerName,
                    category = e.Category,
                    subType = e.SubType,
                    geometryType = e.GeometryType,
                    depth = e.DepthM,
                    diameter = e.DiameterMm,
                    material = e.Material,
                    chainage = e.Chainage
                }
            });

            return Ok(new
            {
                type = "FeatureCollection",
                features = featuresGeoJson,
                bounds = new
                {
                    minX = result.DisplayExtent.MinX,
                    minY = result.DisplayExtent.MinY,
                    maxX = result.DisplayExtent.MaxX,
                    maxY = result.DisplayExtent.MaxY
                }
            });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
            {
                try { System.IO.File.Delete(tempPath); } catch { }
            }
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<DrawingDto>> Upload(Guid workspaceId, [FromForm] UploadDrawingRequestDto request, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Drawing file is required." });
        }

        if (file.Length > 104857600) // 100 MB limit
        {
            return BadRequest(new { message = "File size exceeds 100MB limit." });
        }

        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExts = new[] { ".dxf", ".dwg", ".pdf", ".geojson", ".zip" };
        if (!allowedExts.Contains(ext))
        {
            return BadRequest(new { message = $"Unsupported file format '{ext}'. Allowed: {string.Join(", ", allowedExts)}" });
        }

        var user = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
        var ws = await _context.Workspaces.FindAsync(workspaceId);
        if (ws == null) return NotFound("Workspace not found.");

        int epsg = request.SourceEpsg ?? ws.DefaultEpsg;

        // Save file
        string relativeStoragePath;
        using (var stream = file.OpenReadStream())
        {
            relativeStoragePath = await _fileStorage.SaveFileAsync(stream, file.FileName, $"drawings/{workspaceId}");
        }

        // Handle Level 1 Pin coordinates if provided
        Geometry? pinNative = null;
        Geometry? pinDisplay = null;
        if (request.PinX.HasValue && request.PinY.HasValue)
        {
            // If Pin is in lon/lat (from web map)
            pinDisplay = _geomFactory.CreatePoint(new Coordinate(request.PinX.Value, request.PinY.Value));
            pinNative = _transformer.Transform(pinDisplay, 4326, epsg);
        }

        // Check if drawing already exists by ProjectName & Category & SubType (creating a revision!)
        var existingDrawing = await _context.UploadedDrawings
            .Include(d => d.Revisions)
            .FirstOrDefaultAsync(d => d.WorkspaceId == workspaceId &&
                                      d.ProjectName == request.ProjectName &&
                                      d.Category == request.Category &&
                                      d.SubType == request.SubType);

        UploadedDrawing drawing;
        DrawingRevision revision;

        if (existingDrawing != null)
        {
            drawing = existingDrawing;
            drawing.CurrentRevision = request.RevisionNumber;
            drawing.Status = request.Status;
            drawing.ReviewStatus = ReviewStatus.Pending; // Needs re-approval
            drawing.ProcessingStatus = ext == ".dxf" ? "Queued" : "Completed";
            drawing.ProcessingError = null;

            revision = new DrawingRevision
            {
                DrawingId = drawing.Id,
                WorkspaceId = workspaceId,
                RevisionNumber = request.RevisionNumber,
                StoragePath = relativeStoragePath,
                OriginalFileName = file.FileName,
                ReviewStatus = ReviewStatus.Pending,
                IsActive = false,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            _context.DrawingRevisions.Add(revision);
        }
        else
        {
            drawing = new UploadedDrawing
            {
                WorkspaceId = workspaceId,
                ProjectName = request.ProjectName,
                Category = request.Category,
                SubType = request.SubType,
                CurrentRevision = request.RevisionNumber,
                Status = request.Status,
                ReviewStatus = ReviewStatus.Pending,
                Contractor = request.Contractor,
                Consultant = request.Consultant,
                DrawingDate = request.DrawingDate,
                SourceEpsg = epsg,
                ChainageStart = request.ChainageStart,
                ChainageEnd = request.ChainageEnd,
                OriginalFileName = file.FileName,
                StoragePath = relativeStoragePath,
                FileSize = file.Length,
                FileType = ext,
                PinLocation = pinNative,
                DisplayPinLocation = pinDisplay,
                ProcessingStatus = ext == ".dxf" ? "Queued" : "Completed",
                Notes = request.Notes,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _context.UploadedDrawings.Add(drawing);
            await _context.SaveChangesAsync();

            revision = new DrawingRevision
            {
                DrawingId = drawing.Id,
                WorkspaceId = workspaceId,
                RevisionNumber = request.RevisionNumber,
                StoragePath = relativeStoragePath,
                OriginalFileName = file.FileName,
                ReviewStatus = ReviewStatus.Pending,
                IsActive = false,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            _context.DrawingRevisions.Add(revision);
        }

        await _context.SaveChangesAsync();

        // Enqueue background processing if DXF
        if (ext == ".dxf")
        {
            var jobId = _backgroundJobClient.Enqueue<HangfireDxfProcessingJob>(job =>
                job.ProcessDrawingAsync(drawing.Id, revision.Id, workspaceId));
            drawing.JobId = jobId;
            await _context.SaveChangesAsync();
        }

        await _auditService.LogAsync(workspaceId, "Upload", "UploadedDrawing", drawing.Id.ToString(), $"Uploaded revision {revision.RevisionNumber} of {drawing.ProjectName}", user);

        return Ok(new DrawingDto
        {
            Id = drawing.Id,
            WorkspaceId = drawing.WorkspaceId,
            ProjectName = drawing.ProjectName,
            Category = drawing.Category,
            SubType = drawing.SubType,
            CurrentRevision = drawing.CurrentRevision,
            Status = drawing.Status,
            ReviewStatus = drawing.ReviewStatus,
            Contractor = drawing.Contractor,
            Consultant = drawing.Consultant,
            DrawingDate = drawing.DrawingDate,
            SourceEpsg = drawing.SourceEpsg,
            ChainageStart = drawing.ChainageStart,
            ChainageEnd = drawing.ChainageEnd,
            OriginalFileName = drawing.OriginalFileName,
            FileSize = drawing.FileSize,
            FileType = drawing.FileType,
            ProcessingStatus = drawing.ProcessingStatus,
            Notes = drawing.Notes,
            CreatedBy = drawing.CreatedBy,
            CreatedAt = drawing.CreatedAt
        });
    }

    [HttpPost("{id}/pin")]
    public async Task<IActionResult> SetPinLocation(Guid workspaceId, Guid id, [FromBody] CoordinateDto coord)
    {
        var drawing = await _context.UploadedDrawings.FirstOrDefaultAsync(d => d.WorkspaceId == workspaceId && d.Id == id);
        if (drawing == null) return NotFound();

        var displayPoint = _geomFactory.CreatePoint(new Coordinate(coord.Lon, coord.Lat));
        var nativePoint = _transformer.Transform(displayPoint, 4326, drawing.SourceEpsg);

        drawing.DisplayPinLocation = displayPoint;
        drawing.PinLocation = nativePoint;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Pin location updated." });
    }

    [HttpGet("{id}/file")]
    public async Task<IActionResult> DownloadFile(Guid workspaceId, Guid id)
    {
        var drawing = await _context.UploadedDrawings.FirstOrDefaultAsync(d => d.WorkspaceId == workspaceId && d.Id == id);
        if (drawing == null) return NotFound();

        var stream = await _fileStorage.OpenReadAsync(drawing.StoragePath);
        return File(stream, "application/octet-stream", drawing.OriginalFileName);
    }
}

public class CoordinateDto
{
    public double Lon { get; set; }
    public double Lat { get; set; }
}
