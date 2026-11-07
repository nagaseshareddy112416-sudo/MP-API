using GfcEngine.Domain.Enums;

namespace GfcEngine.Application.DTOs;

public class DrawingStageDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsGfcBaseline { get; set; }
    public bool IsAsBuilt { get; set; }
    public string? Description { get; set; }
}

public class CreateDrawingStageDto
{
    public string Name { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsGfcBaseline { get; set; }
    public bool IsAsBuilt { get; set; }
    public string? Description { get; set; }
}

public class ToleranceRuleDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public double HorizontalToleranceM { get; set; }
    public double VerticalToleranceM { get; set; }
    public double LengthToleranceM { get; set; }
    public string? AttributeRulesJson { get; set; }
    public string? Notes { get; set; }
}

public class FeatureMatchDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid AsBuiltDrawingId { get; set; }
    public Guid GfcDrawingId { get; set; }
    public Guid? AsBuiltFeatureId { get; set; }
    public Guid? GfcFeatureId { get; set; }
    public FeatureMatchType MatchType { get; set; }
    public DateTime MatchedAt { get; set; }
}

public class ComplianceResultDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid AsBuiltDrawingId { get; set; }
    public string AsBuiltDrawingName { get; set; } = string.Empty;
    public Guid GfcDrawingId { get; set; }
    public string GfcDrawingName { get; set; } = string.Empty;
    public Guid GfcRevisionId { get; set; }
    public string GfcRevisionNumber { get; set; } = string.Empty;
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
    public ComplianceStatus Status { get; set; }
    public DateTime ComputedAt { get; set; }
    public CoordinateSource? SourceOfCoordinates { get; set; }
    public bool HasNonSurveyWarning { get; set; }
    public List<DeviationDto> Deviations { get; set; } = new();
}

public class DeviationDto
{
    public Guid Id { get; set; }
    public Guid ComplianceResultId { get; set; }
    public DeviationDecision Decision { get; set; }
    public string? Reason { get; set; }
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comment { get; set; }
}

public class ReviewDeviationDto
{
    public DeviationDecision Decision { get; set; }
    public string Reason { get; set; } = string.Empty; // Mandatory
    public string? Comment { get; set; }
}

public class ComplianceSummaryDto
{
    public Guid WorkspaceId { get; set; }
    public int TotalFeatures { get; set; }
    public int CompliantCount { get; set; }
    public int WithinToleranceCount { get; set; }
    public int DeviationCount { get; set; }
    public int MissingCount { get; set; }
    public int ExtraCount { get; set; }
    public double CompliancePercentage { get; set; }
    public int OpenDeviationsCount { get; set; }
    public int AcceptedDeviationsCount { get; set; }
    public Dictionary<string, CategoryComplianceDto> ByCategory { get; set; } = new();
}

public class CategoryComplianceDto
{
    public string Category { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Compliant { get; set; }
    public int WithinTolerance { get; set; }
    public int Deviations { get; set; }
    public int Missing { get; set; }
    public int Extra { get; set; }
    public double CompliancePercentage { get; set; }
}

