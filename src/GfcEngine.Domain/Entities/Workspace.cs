using NetTopologySuite.Geometries;
using GfcEngine.Domain.Common;

namespace GfcEngine.Domain.Entities;

public class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DefaultEpsg { get; set; } = 32644;
    public int DisplayEpsg { get; set; } = 4326;
    public double CenterLon { get; set; } = 80.516;
    public double CenterLat { get; set; } = 16.513;
    public int DefaultZoom { get; set; } = 14;
    public double ClashVerticalToleranceM { get; set; } = 0.5;
    public double FarFromExistingDataWarningM { get; set; } = 5000.0;
    public Geometry? BoundaryGeometry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CadLayerMapping> CadLayerMappings { get; set; } = new List<CadLayerMapping>();
    public ICollection<CategoryConfig> CategoryConfigs { get; set; } = new List<CategoryConfig>();
    public ICollection<CategoryFieldConfig> CategoryFieldConfigs { get; set; } = new List<CategoryFieldConfig>();
    public ICollection<UploadedDrawing> Drawings { get; set; } = new List<UploadedDrawing>();
    public ICollection<Clash> Clashes { get; set; } = new List<Clash>();
    public ICollection<ReferenceLayer> ReferenceLayers { get; set; } = new List<ReferenceLayer>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<WorkspaceUserRole> UserRoles { get; set; } = new List<WorkspaceUserRole>();
}
