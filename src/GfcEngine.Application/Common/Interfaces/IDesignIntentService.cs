using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;

namespace GfcEngine.Application.Common.Interfaces;

public interface IDesignIntentService
{
    Task<DesignModelDto> ProcessAndSaveModelAsync(
        Guid workspaceId,
        Guid? drawingId,
        string fileName,
        Stream fileStream,
        string createdBy,
        CancellationToken ct = default);

    Task<SnapFootprintResultDto> SnapToFootprintAsync(
        Guid workspaceId,
        Guid modelId,
        Guid? drawingId = null,
        CancellationToken ct = default);

    Task<DesignModelDto> UpdatePlacementAsync(
        Guid workspaceId,
        Guid modelId,
        UpdateModelPlacementDto dto,
        CancellationToken ct = default);

    Task<DesignModelDto> ConvertModelUnitsAsync(
        Guid workspaceId,
        Guid modelId,
        string fromUnit,
        string toUnit,
        CancellationToken ct = default);

    Task<List<DesignModelDto>> GetDesignModelsAsync(Guid workspaceId, CancellationToken ct = default);

    Task<DrapedRenderDto> SaveDrapedRenderAsync(
        Guid workspaceId,
        Guid? drawingId,
        string title,
        string fileName,
        Stream fileStream,
        double westLon,
        double southLat,
        double eastLon,
        double northLat,
        string createdBy,
        CancellationToken ct = default);

    Task<List<DrapedRenderDto>> GetDrapedRendersAsync(Guid workspaceId, CancellationToken ct = default);

    Task<List<PlantLibraryItemDto>> GetPlantLibraryAsync(Guid workspaceId, CancellationToken ct = default);

    Task<ElevationPerspectiveImageDto> SaveElevationPerspectiveAsync(
        Guid workspaceId,
        Guid? drawingId,
        string title,
        string imageType,
        string fileName,
        Stream fileStream,
        double? camLon,
        double? camLat,
        double? camHeight,
        double? camHeading,
        double? camPitch,
        string createdBy,
        CancellationToken ct = default);

    Task<List<ElevationPerspectiveImageDto>> GetElevationsAndPerspectivesAsync(Guid workspaceId, CancellationToken ct = default);

    Task<StageViewsDataDto> GetStageViewsDataAsync(Guid workspaceId, CancellationToken ct = default);
}
