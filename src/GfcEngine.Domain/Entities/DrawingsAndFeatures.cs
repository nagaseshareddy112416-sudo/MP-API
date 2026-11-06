using NetTopologySuite.Geometries;
using GfcEngine.Domain.Common;
using GfcEngine.Domain.Enums;

namespace GfcEngine.Domain.Entities;

public class UploadedDrawing : WorkspaceScopedEntity
{
    public string ProjectName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string CurrentRevision { get; set; } = "R0";
    public DrawingStatus Status { get; set; } = DrawingStatus.Design;
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Pending;
    public string? Contractor { get; set; }
    public string? Consultant { get; set; }
    public DateTime? DrawingDate { get; set; }
    public int SourceEpsg { get; set; } = 32644;
    public double? ChainageStart { get; set; }
    public double? ChainageEnd { get; set; }
    public Geometry? PinLocation { get; set; } // For Level 1 placement (EPSG:32644)
    public Geometry? DisplayPinLocation { get; set; } // EPSG:4326
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public string ProcessingStatus { get; set; } = "Pending"; // Pending, Queued, Processing, Completed, Failed
    public string? ProcessingError { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace? Workspace { get; set; }
    public ICollection<DrawingRevision> Revisions { get; set; } = new List<DrawingRevision>();
    public ICollection<ExtractedFeature> Features { get; set; } = new List<ExtractedFeature>();
}

public class DrawingRevision : WorkspaceScopedEntity
{
    public Guid DrawingId { get; set; }
    public string RevisionNumber { get; set; } = "R0";
    public string StoragePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Pending;
    public string? ReviewComments { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UploadedDrawing? Drawing { get; set; }
    public Workspace? Workspace { get; set; }
    public ICollection<ExtractedFeature> Features { get; set; } = new List<ExtractedFeature>();
}

public class ExtractedFeature : WorkspaceScopedEntity
{
    public Guid DrawingId { get; set; }
    public Guid RevisionId { get; set; }
    public string CadLayer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string GeometryType { get; set; } = string.Empty; // "line", "point", "polygon"
    public Geometry NativeGeometry { get; set; } = null!; // Projected working geometry (EPSG:32644)
    public Geometry DisplayGeometry { get; set; } = null!; // Display geometry (EPSG:4326)
    public double? Z { get; set; }
    public double? DepthM { get; set; }
    public int? DiameterMm { get; set; }
    public string? Material { get; set; }
    public double? Chainage { get; set; }
    public string? AttributesJson { get; set; }
    public bool IsActive { get; set; } = false; // True only for approved latest revision

    public UploadedDrawing? Drawing { get; set; }
    public DrawingRevision? Revision { get; set; }
    public Workspace? Workspace { get; set; }
}

