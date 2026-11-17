namespace GfcEngine.Application.DTOs;

public class DesignModelDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? DrawingId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? OptimizedStoragePath { get; set; }
    public string FileType { get; set; } = "GLB";
    public long OriginalFileSize { get; set; }
    public long? OptimizedFileSize { get; set; }
    public int PolygonCount { get; set; }
    public int VertexCount { get; set; }
    public double WidthM { get; set; }
    public double DepthM { get; set; }
    public double HeightM { get; set; }
    public string DetectedUnit { get; set; } = "m";
    public bool UnitCorrectionApplied { get; set; }
    public List<string> MissingTextures { get; set; } = new();
    public List<string> UnsupportedMaterials { get; set; } = new();

    public double PositionLon { get; set; }
    public double PositionLat { get; set; }
    public double ElevationOffsetM { get; set; }
    public double RotationDegrees { get; set; }
    public double Scale { get; set; } = 1.0;
    public bool SnapToFootprint { get; set; } = true;
    public bool FlattenTerrain { get; set; } = true;
    public bool ClampToGround { get; set; } = true;

    public string ValidationStatus { get; set; } = "Valid";
    public string? ValidationMessage { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateModelPlacementDto
{
    public double PositionLon { get; set; }
    public double PositionLat { get; set; }
    public double ElevationOffsetM { get; set; }
    public double RotationDegrees { get; set; }
    public double Scale { get; set; } = 1.0;
    public bool SnapToFootprint { get; set; }
    public bool FlattenTerrain { get; set; }
    public bool ClampToGround { get; set; }
}

public class DrapedRenderDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? DrawingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public double WestLon { get; set; }
    public double SouthLat { get; set; }
    public double EastLon { get; set; }
    public double NorthLat { get; set; }
    public double RotationDegrees { get; set; }
    public double Opacity { get; set; } = 0.85;
    public double AlignmentErrorM { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlantLibraryItemDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string CadBlockName { get; set; } = string.Empty;
    public string CommonName { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string ModelType { get; set; } = "3D_GLB";
    public string ModelAssetPath { get; set; } = string.Empty;
    public double DefaultHeightM { get; set; } = 6.0;
    public double DefaultCrownM { get; set; } = 4.0;
    public string LicenseName { get; set; } = "CC0 Public Domain";
    public string LicenseSource { get; set; } = "Open Assets / Synthetics";
}

public class SiteLandscapeLayerDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? DrawingId { get; set; }
    public string CadLayer { get; set; } = string.Empty;
    public string SurfaceType { get; set; } = "Lawn";
    public string TextureName { get; set; } = "grass_pbr";
    public double Roughness { get; set; } = 0.8;
    public double Metallic { get; set; } = 0.0;
    public string ColorHex { get; set; } = "#4caf50";
    public bool Draped { get; set; } = true;
}

public class ElevationPerspectiveImageDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? DrawingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageType { get; set; } = "Elevation";
    public string StoragePath { get; set; } = string.Empty;
    public double? CameraLon { get; set; }
    public double? CameraLat { get; set; }
    public double? CameraHeightM { get; set; }
    public double? CameraHeadingDeg { get; set; }
    public double? CameraPitchDeg { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SnapFootprintResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double CenterLon { get; set; }
    public double CenterLat { get; set; }
    public double FootprintWidthM { get; set; }
    public double FootprintDepthM { get; set; }
    public double OrientationAngleDegrees { get; set; }
    public double ModelWidthM { get; set; }
    public double ModelDepthM { get; set; }
    public double DimensionRatio { get; set; }
    public bool UnitMismatchDetected { get; set; }
    public string RecommendedAction { get; set; } = "None";
}

public class StageViewsDataDto
{
    public List<DesignModelDto> DesignModels { get; set; } = new();
    public List<DrapedRenderDto> DrapedRenders { get; set; } = new();
    public List<SiteLandscapeLayerDto> LandscapeLayers { get; set; } = new();
    public List<PlantLibraryItemDto> PlantLibrary { get; set; } = new();
    public List<ElevationPerspectiveImageDto> ElevationsAndPerspectives { get; set; } = new();
    public List<UnderConstructionComponentDto> ConstructionComponents { get; set; } = new();
    public List<AsBuiltRealityFeatureDto> AsBuiltFeatures { get; set; } = new();
}

public class UnderConstructionComponentDto
{
    public Guid FeatureId { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string ConstructionStatus { get; set; } = "NotStarted"; // NotStarted, InProgress, Completed, Defect
    public string StatusColorHex { get; set; } = "#3b82f6";
    public double ProgressPercent { get; set; }
    public double CenterLon { get; set; }
    public double CenterLat { get; set; }
}

public class AsBuiltRealityFeatureDto
{
    public Guid FeatureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double DeviationM { get; set; }
    public string ComplianceStatus { get; set; } = "Compliant";
    public double CenterLon { get; set; }
    public double CenterLat { get; set; }
}
