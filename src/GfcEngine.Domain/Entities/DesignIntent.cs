using GfcEngine.Domain.Common;

namespace GfcEngine.Domain.Entities;

public class DesignModel : WorkspaceScopedEntity
{
    public Guid? DrawingId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? OptimizedStoragePath { get; set; }
    public string FileType { get; set; } = "GLB"; // GLB, glTF, OBJ, IFC
    public long OriginalFileSize { get; set; }
    public long? OptimizedFileSize { get; set; }
    public int PolygonCount { get; set; }
    public int VertexCount { get; set; }
    public double WidthM { get; set; }
    public double DepthM { get; set; }
    public double HeightM { get; set; }
    public string DetectedUnit { get; set; } = "m"; // m, mm, cm, ft
    public bool UnitCorrectionApplied { get; set; }
    public string? MissingTexturesJson { get; set; }
    public string? UnsupportedMaterialsJson { get; set; }

    // Geographic placement
    public double PositionLon { get; set; }
    public double PositionLat { get; set; }
    public double ElevationOffsetM { get; set; }
    public double RotationDegrees { get; set; } // Yaw in degrees from North
    public double Scale { get; set; } = 1.0;
    public bool SnapToFootprint { get; set; } = true;
    public bool FlattenTerrain { get; set; } = true;
    public bool ClampToGround { get; set; } = true;

    // Validation and status
    public string ValidationStatus { get; set; } = "Valid"; // Valid, Warning, Error
    public string? ValidationMessage { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace? Workspace { get; set; }
    public UploadedDrawing? Drawing { get; set; }
}

public class DrapedRender : WorkspaceScopedEntity
{
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
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace? Workspace { get; set; }
    public UploadedDrawing? Drawing { get; set; }
}

public class PlantLibraryItem : WorkspaceScopedEntity
{
    public string CadBlockName { get; set; } = string.Empty; // e.g., TREE, PALM, SHRUB
    public string CommonName { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string ModelType { get; set; } = "3D_GLB"; // 3D_GLB, Billboard_PNG
    public string ModelAssetPath { get; set; } = string.Empty;
    public double DefaultHeightM { get; set; } = 6.0;
    public double DefaultCrownM { get; set; } = 4.0;
    public string LicenseName { get; set; } = "CC0 Public Domain";
    public string LicenseSource { get; set; } = "Open Assets / Synthetics";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace? Workspace { get; set; }
}

public class SiteLandscapeLayer : WorkspaceScopedEntity
{
    public Guid? DrawingId { get; set; }
    public string CadLayer { get; set; } = string.Empty;
    public string SurfaceType { get; set; } = "Lawn"; // Lawn, Paving, Garden, Water, Steps
    public string TextureName { get; set; } = "grass_pbr";
    public double Roughness { get; set; } = 0.8;
    public double Metallic { get; set; } = 0.0;
    public string ColorHex { get; set; } = "#4caf50";
    public bool Draped { get; set; } = true;

    public Workspace? Workspace { get; set; }
    public UploadedDrawing? Drawing { get; set; }
}

public class ElevationPerspectiveImage : WorkspaceScopedEntity
{
    public Guid? DrawingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageType { get; set; } = "Elevation"; // Elevation, Perspective, Detail
    public string StoragePath { get; set; } = string.Empty;
    public double? CameraLon { get; set; }
    public double? CameraLat { get; set; }
    public double? CameraHeightM { get; set; }
    public double? CameraHeadingDeg { get; set; }
    public double? CameraPitchDeg { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace? Workspace { get; set; }
    public UploadedDrawing? Drawing { get; set; }
}
