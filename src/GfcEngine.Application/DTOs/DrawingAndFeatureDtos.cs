using GfcEngine.Domain.Enums;

namespace GfcEngine.Application.DTOs;

public class UploadDrawingRequestDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string RevisionNumber { get; set; } = "R0";
    public DrawingStatus Status { get; set; } = DrawingStatus.Design;
    public string? Contractor { get; set; }
    public string? Consultant { get; set; }
    public DateTime? DrawingDate { get; set; }
    public int? SourceEpsg { get; set; }
    public double? ChainageStart { get; set; }
    public double? ChainageEnd { get; set; }
    public string? Notes { get; set; }
    public double? PinX { get; set; }
    public double? PinY { get; set; }
}

public class DrawingDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string CurrentRevision { get; set; } = "R0";
    public DrawingStatus Status { get; set; }
    public ReviewStatus ReviewStatus { get; set; }
    public string? Contractor { get; set; }
    public string? Consultant { get; set; }
    public DateTime? DrawingDate { get; set; }
    public int SourceEpsg { get; set; }
    public double? ChainageStart { get; set; }
    public double? ChainageEnd { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string ProcessingStatus { get; set; } = string.Empty;
    public string? ProcessingError { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public double? PinLon { get; set; }
    public double? PinLat { get; set; }
}

public class RevisionDto
{
    public Guid Id { get; set; }
    public Guid DrawingId { get; set; }
    public string RevisionNumber { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public ReviewStatus ReviewStatus { get; set; }
    public string? ReviewComments { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int FeatureCount { get; set; }
}

public class ReviewDecisionDto
{
    public bool Approved { get; set; }
    public string? Comments { get; set; }
}

public class ClashDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid FeatureAId { get; set; }
    public Guid FeatureBId { get; set; }
    public string FeatureACategory { get; set; } = string.Empty;
    public string FeatureASubType { get; set; } = string.Empty;
    public double? FeatureADepth { get; set; }
    public string FeatureBCategory { get; set; } = string.Empty;
    public string FeatureBSubType { get; set; } = string.Empty;
    public double? FeatureBDepth { get; set; }
    public double Lon { get; set; }
    public double Lat { get; set; }
    public double VerticalGapM { get; set; }
    public ClashStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime DetectedAt { get; set; }
}

public class UpdateClashStatusDto
{
    public ClashStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class ReferenceLayerDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Color { get; set; } = "#3f51b5";
    public bool IsVisible { get; set; }
    public string? GeoJsonData { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardStatsDto
{
    public int TotalDrawings { get; set; }
    public int TotalFeatures { get; set; }
    public int TotalClashes { get; set; }
    public int OpenClashes { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public Dictionary<string, int> CategoryCounts { get; set; } = new();
    public List<RoadChainageProgressDto> RoadProgress { get; set; } = new();
    public List<DrawingDto> RecentUploads { get; set; } = new();
}

public class RoadChainageProgressDto
{
    public string RoadName { get; set; } = string.Empty;
    public double TotalLengthM { get; set; }
    public double CompletedLengthM { get; set; }
    public double PercentComplete { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
