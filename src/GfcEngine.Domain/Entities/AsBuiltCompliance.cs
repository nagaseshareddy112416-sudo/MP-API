using GfcEngine.Domain.Common;
using GfcEngine.Domain.Enums;

namespace GfcEngine.Domain.Entities;

public class DrawingStage : WorkspaceScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsGfcBaseline { get; set; }
    public bool IsAsBuilt { get; set; }
    public string? Description { get; set; }

    public Workspace? Workspace { get; set; }
}

public class ToleranceRule : WorkspaceScopedEntity
{
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public double HorizontalToleranceM { get; set; } = 0.10; // Editable placeholder
    public double VerticalToleranceM { get; set; } = 0.05;   // Editable placeholder
    public double LengthToleranceM { get; set; } = 0.50;     // Editable placeholder
    public string? AttributeRulesJson { get; set; }
    public string? Notes { get; set; }

    public Workspace? Workspace { get; set; }
}

public class FeatureMatch : WorkspaceScopedEntity
{
    public Guid AsBuiltDrawingId { get; set; }
    public Guid GfcDrawingId { get; set; }
    public Guid? AsBuiltFeatureId { get; set; }
    public Guid? GfcFeatureId { get; set; }
    public FeatureMatchType MatchType { get; set; } = FeatureMatchType.Automatic;
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

    public UploadedDrawing? AsBuiltDrawing { get; set; }
    public UploadedDrawing? GfcDrawing { get; set; }
    public ExtractedFeature? AsBuiltFeature { get; set; }
    public ExtractedFeature? GfcFeature { get; set; }
    public Workspace? Workspace { get; set; }
}

public class ComplianceResult : WorkspaceScopedEntity
{
    public Guid AsBuiltDrawingId { get; set; }
    public Guid GfcDrawingId { get; set; }
    public Guid GfcRevisionId { get; set; }
    public Guid? FeatureMatchId { get; set; }
    public Guid? AsBuiltFeatureId { get; set; }
    public Guid? GfcFeatureId { get; set; }

    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public double? ChainageStart { get; set; }
    public double? ChainageEnd { get; set; }

    public double HorizontalDeviationM { get; set; }
    public double AverageHorizontalDeviationM { get; set; }
    public double VerticalDeviationM { get; set; }
    public double LengthDeviationM { get; set; }
    public string? AttributeDiffsJson { get; set; }

    public ComplianceStatus Status { get; set; } = ComplianceStatus.Compliant;
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    public UploadedDrawing? AsBuiltDrawing { get; set; }
    public UploadedDrawing? GfcDrawing { get; set; }
    public DrawingRevision? GfcRevision { get; set; }
    public FeatureMatch? FeatureMatch { get; set; }
    public ExtractedFeature? AsBuiltFeature { get; set; }
    public ExtractedFeature? GfcFeature { get; set; }
    public Workspace? Workspace { get; set; }

    public ICollection<Deviation> Deviations { get; set; } = new List<Deviation>();
}

public class Deviation : WorkspaceScopedEntity
{
    public Guid ComplianceResultId { get; set; }
    public DeviationDecision Decision { get; set; } = DeviationDecision.Pending;
    public string? Reason { get; set; } // Mandatory when accepted or rejected
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comment { get; set; }

    public ComplianceResult? ComplianceResult { get; set; }
    public Workspace? Workspace { get; set; }
}

