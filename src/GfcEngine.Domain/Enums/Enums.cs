namespace GfcEngine.Domain.Enums;

public enum DrawingStatus
{
    Design,
    Approved,
    UnderConstruction,
    Completed
}

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}

public enum ClashStatus
{
    Open,
    Resolved,
    Ignored
}

public enum ComplianceStatus
{
    Compliant,
    WithinTolerance,
    Deviation,
    Missing,
    Extra
}

public enum DeviationDecision
{
    Pending,
    Accepted,
    Rectify,
    Rejected
}

public enum CoordinateSource
{
    Survey,
    Redrawn,
    Unknown
}

public enum FeatureMatchType
{
    Automatic,
    Manual
}

public enum ConstructionStatus
{
    NotStarted,
    UnderConstruction,
    Completed
}

public static class Roles
{
    public const string WorkspaceAdmin = "WorkspaceAdmin";
    public const string Uploader = "Uploader";
    public const string Reviewer = "Reviewer";
    public const string Viewer = "Viewer";
}

