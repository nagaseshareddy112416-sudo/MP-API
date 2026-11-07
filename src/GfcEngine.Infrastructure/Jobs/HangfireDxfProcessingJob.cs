using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.Infrastructure.Jobs;

public class HangfireDxfProcessingJob
{
    private readonly ApplicationDbContext _context;
    private readonly IDxfExtractionService _extractionService;
    private readonly IFileStorage _fileStorage;
    private readonly IAuditService _auditService;
    private readonly ILogger<HangfireDxfProcessingJob> _logger;

    public HangfireDxfProcessingJob(
        ApplicationDbContext context,
        IDxfExtractionService extractionService,
        IFileStorage fileStorage,
        IAuditService auditService,
        ILogger<HangfireDxfProcessingJob> logger)
    {
        _context = context;
        _extractionService = extractionService;
        _fileStorage = fileStorage;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task ProcessDrawingAsync(Guid drawingId, Guid revisionId, Guid workspaceId)
    {
        _logger.LogInformation("Starting background processing for Drawing {DrawingId}, Revision {RevisionId}", drawingId, revisionId);

        var drawing = await _context.UploadedDrawings.FirstOrDefaultAsync(d => d.Id == drawingId);
        var revision = await _context.DrawingRevisions.FirstOrDefaultAsync(r => r.Id == revisionId);

        if (drawing == null || revision == null)
        {
            _logger.LogError("Drawing {DrawingId} or Revision {RevisionId} not found.", drawingId, revisionId);
            return;
        }

        drawing.ProcessingStatus = "Processing";
        await _context.SaveChangesAsync();

        try
        {
            var fullPath = _fileStorage.GetFullPath(revision.StoragePath);
            var extractResult = await _extractionService.ExtractAsync(fullPath, drawing.SourceEpsg, workspaceId);

            if (!extractResult.Success)
            {
                drawing.ProcessingStatus = "Failed";
                drawing.ProcessingError = extractResult.ErrorMessage;
                await _context.SaveChangesAsync();
                await _auditService.LogAsync(workspaceId, "ExtractionFailed", "UploadedDrawing", drawingId.ToString(), extractResult.ErrorMessage, drawing.CreatedBy);
                _logger.LogWarning("Extraction failed for drawing {DrawingId}: {Error}", drawingId, extractResult.ErrorMessage);
                return;
            }

            bool isAsBuilt = drawing.Stage?.IsAsBuilt == true || string.Equals(drawing.StageName, "As-Built", StringComparison.OrdinalIgnoreCase);

            // Save extracted features
            var features = extractResult.Entities.Select(e => new ExtractedFeature
            {
                WorkspaceId = workspaceId,
                DrawingId = drawingId,
                RevisionId = revisionId,
                CadLayer = e.LayerName,
                Category = e.Category,
                SubType = e.SubType,
                GeometryType = e.GeometryType,
                NativeGeometry = e.NativeGeometry,
                DisplayGeometry = e.DisplayGeometry,
                Z = e.Z,
                DepthM = e.DepthM,
                DiameterMm = e.DiameterMm,
                Material = e.Material,
                Chainage = e.Chainage,
                IsAsBuilt = isAsBuilt,
                ConstructionStatus = isAsBuilt ? ConstructionStatus.UnderConstruction : ConstructionStatus.NotStarted,
                IsActive = false // Features become active once the drawing revision is approved!
            }).ToList();

            _context.ExtractedFeatures.AddRange(features);
            drawing.ProcessingStatus = "Completed";
            drawing.ProcessingError = null;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(workspaceId, "ExtractionCompleted", "UploadedDrawing", drawingId.ToString(), $"Extracted {features.Count} features.", drawing.CreatedBy);
            _logger.LogInformation("Extracted and saved {Count} features for drawing {DrawingId}", features.Count, drawingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during background processing of drawing {DrawingId}", drawingId);
            drawing.ProcessingStatus = "Failed";
            drawing.ProcessingError = ex.Message;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(workspaceId, "ExtractionError", "UploadedDrawing", drawingId.ToString(), ex.Message, drawing.CreatedBy);
        }
    }
}

