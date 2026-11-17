using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Application.DTOs;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/design-intent")]
[Authorize]
public class DesignIntentController : ControllerBase
{
    private readonly IDesignIntentService _designIntentService;
    private readonly IAuditService _auditService;

    public DesignIntentController(
        IDesignIntentService designIntentService,
        IAuditService auditService)
    {
        _designIntentService = designIntentService;
        _auditService = auditService;
    }

    [HttpPost("models/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DesignModelDto>> UploadModel(
        Guid workspaceId,
        [FromForm] IFormFile file,
        [FromForm] Guid? drawingId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No 3D model file provided." });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext == ".skp" || ext == ".rvt")
        {
            return BadRequest(new
            {
                message = $"Proprietary CAD format '{ext}' is not supported directly.",
                guidance = "Please export from Revit using 'File > Export > IFC' or 'Revit glTF Exporter', or from SketchUp using 'File > Export > 3D Model > glTF (.glb)'."
            });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            await using var stream = file.OpenReadStream();

            var result = await _designIntentService.ProcessAndSaveModelAsync(
                workspaceId,
                drawingId,
                file.FileName,
                stream,
                userId);

            await _auditService.LogAsync(
                workspaceId,
                "UPLOAD_DESIGN_MODEL",
                "DesignModel",
                result.Id.ToString(),
                $"Uploaded 3D design model '{file.FileName}' ({result.FileType}, {result.PolygonCount} polys)");

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("models")]
    public async Task<ActionResult<List<DesignModelDto>>> GetModels(Guid workspaceId)
    {
        var list = await _designIntentService.GetDesignModelsAsync(workspaceId);
        return Ok(list);
    }

    [HttpPost("models/{modelId}/snap-to-footprint")]
    public async Task<ActionResult<SnapFootprintResultDto>> SnapToFootprint(
        Guid workspaceId,
        Guid modelId,
        [FromQuery] Guid? drawingId)
    {
        var result = await _designIntentService.SnapToFootprintAsync(workspaceId, modelId, drawingId);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPatch("models/{modelId}/placement")]
    public async Task<ActionResult<DesignModelDto>> UpdatePlacement(
        Guid workspaceId,
        Guid modelId,
        [FromBody] UpdateModelPlacementDto dto)
    {
        try
        {
            var updated = await _designIntentService.UpdatePlacementAsync(workspaceId, modelId, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("models/{modelId}/convert-units")]
    public async Task<ActionResult<DesignModelDto>> ConvertUnits(
        Guid workspaceId,
        Guid modelId,
        [FromQuery] string fromUnit = "mm",
        [FromQuery] string toUnit = "m")
    {
        try
        {
            var updated = await _designIntentService.ConvertModelUnitsAsync(workspaceId, modelId, fromUnit, toUnit);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("draped-renders/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DrapedRenderDto>> UploadDrapedRender(
        Guid workspaceId,
        [FromForm] IFormFile file,
        [FromForm] string? title,
        [FromForm] Guid? drawingId,
        [FromForm] double westLon = 80.5145,
        [FromForm] double southLat = 16.5120,
        [FromForm] double eastLon = 80.5160,
        [FromForm] double northLat = 16.5135)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No render image file provided." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        await using var stream = file.OpenReadStream();

        var result = await _designIntentService.SaveDrapedRenderAsync(
            workspaceId,
            drawingId,
            title ?? file.FileName,
            file.FileName,
            stream,
            westLon,
            southLat,
            eastLon,
            northLat,
            userId);

        return Ok(result);
    }

    [HttpGet("draped-renders")]
    public async Task<ActionResult<List<DrapedRenderDto>>> GetDrapedRenders(Guid workspaceId)
    {
        var list = await _designIntentService.GetDrapedRendersAsync(workspaceId);
        return Ok(list);
    }

    [HttpGet("plant-library")]
    public async Task<ActionResult<List<PlantLibraryItemDto>>> GetPlantLibrary(Guid workspaceId)
    {
        var list = await _designIntentService.GetPlantLibraryAsync(workspaceId);
        return Ok(list);
    }

    [HttpPost("elevations/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ElevationPerspectiveImageDto>> UploadElevationPerspective(
        Guid workspaceId,
        [FromForm] IFormFile file,
        [FromForm] string? title,
        [FromForm] string imageType = "Elevation",
        [FromForm] Guid? drawingId = null,
        [FromForm] double? camLon = null,
        [FromForm] double? camLat = null,
        [FromForm] double? camHeight = null,
        [FromForm] double? camHeading = null,
        [FromForm] double? camPitch = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No image file provided." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        await using var stream = file.OpenReadStream();

        var result = await _designIntentService.SaveElevationPerspectiveAsync(
            workspaceId,
            drawingId,
            title ?? file.FileName,
            imageType,
            file.FileName,
            stream,
            camLon,
            camLat,
            camHeight,
            camHeading,
            camPitch,
            userId);

        return Ok(result);
    }

    [HttpGet("elevations")]
    public async Task<ActionResult<List<ElevationPerspectiveImageDto>>> GetElevationsAndPerspectives(Guid workspaceId)
    {
        var list = await _designIntentService.GetElevationsAndPerspectivesAsync(workspaceId);
        return Ok(list);
    }

    [HttpGet("stage-views")]
    public async Task<ActionResult<StageViewsDataDto>> GetStageViews(Guid workspaceId)
    {
        var data = await _designIntentService.GetStageViewsDataAsync(workspaceId);
        return Ok(data);
    }
}
