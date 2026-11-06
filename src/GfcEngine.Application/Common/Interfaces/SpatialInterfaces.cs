using NetTopologySuite.Geometries;
using GfcEngine.Domain.Entities;

namespace GfcEngine.Application.Common.Interfaces;

public interface ICrsTransformer
{
    Coordinate Transform(double x, double y, int fromEpsg, int toEpsg);
    Geometry Transform(Geometry geometry, int fromEpsg, int toEpsg);
}

public class ExtractedEntityDto
{
    public string LayerName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string GeometryType { get; set; } = string.Empty; // "line", "point", "polygon"
    public Geometry NativeGeometry { get; set; } = null!;
    public Geometry DisplayGeometry { get; set; } = null!;
    public double? Z { get; set; }
    public double? DepthM { get; set; }
    public int? DiameterMm { get; set; }
    public string? Material { get; set; }
    public double? Chainage { get; set; }
    public Dictionary<string, object?> Attributes { get; set; } = new();
}

public class DxfExtractionResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ExtractedEntityDto> Entities { get; set; } = new();
    public List<string> UnmappedLayers { get; set; } = new();
    public Envelope NativeExtent { get; set; } = new();
    public Envelope DisplayExtent { get; set; } = new();
}

public interface IDxfExtractionService
{
    Task<DxfExtractionResult> ExtractAsync(string filePath, int sourceEpsg, Guid workspaceId, CancellationToken cancellationToken = default);
}

public interface IClashDetectionService
{
    Task<List<Clash>> DetectClashesForRevisionAsync(Guid revisionId, Guid workspaceId, double verticalToleranceM, CancellationToken cancellationToken = default);
}
