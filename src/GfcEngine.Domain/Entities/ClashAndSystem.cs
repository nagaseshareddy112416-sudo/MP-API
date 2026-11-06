using NetTopologySuite.Geometries;
using GfcEngine.Domain.Common;
using GfcEngine.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace GfcEngine.Domain.Entities;

public class Clash : WorkspaceScopedEntity
{
    public Guid FeatureAId { get; set; }
    public Guid FeatureBId { get; set; }
    public Guid DrawingAId { get; set; }
    public Guid DrawingBId { get; set; }
    public Geometry Location { get; set; } = null!; // Native Point (EPSG:32644)
    public Geometry DisplayLocation { get; set; } = null!; // Display Point (EPSG:4326)
    public double VerticalGapM { get; set; }
    public ClashStatus Status { get; set; } = ClashStatus.Open;
    public string? Notes { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }

    public ExtractedFeature? FeatureA { get; set; }
    public ExtractedFeature? FeatureB { get; set; }
    public UploadedDrawing? DrawingA { get; set; }
    public UploadedDrawing? DrawingB { get; set; }
    public Workspace? Workspace { get; set; }
}

public class ReferenceLayer : WorkspaceScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string Format { get; set; } = "GeoJSON"; // GeoJSON, Shapefile, KML
    public string StoragePath { get; set; } = string.Empty;
    public Geometry? DisplayGeometry { get; set; } // EPSG:4326
    public string? GeoJsonData { get; set; }
    public string Color { get; set; } = "#3f51b5";
    public bool IsVisible { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace? Workspace { get; set; }
}

public class AuditLog : WorkspaceScopedEntity
{
    public string Action { get; set; } = string.Empty; // "Upload", "Approve", "Reject", "Create", "Update", "Delete"
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Workspace? Workspace { get; set; }
}

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkspaceUserRole> WorkspaceRoles { get; set; } = new List<WorkspaceUserRole>();
}

public class WorkspaceUserRole : WorkspaceScopedEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Viewer;

    public ApplicationUser? User { get; set; }
    public Workspace? Workspace { get; set; }
}

