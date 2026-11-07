using Microsoft.EntityFrameworkCore;
using GfcEngine.Domain.Entities;

namespace GfcEngine.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Workspace> Workspaces { get; }
    DbSet<CadLayerMapping> CadLayerMappings { get; }
    DbSet<CategoryConfig> CategoryConfigs { get; }
    DbSet<CategoryFieldConfig> CategoryFieldConfigs { get; }
    DbSet<UploadedDrawing> UploadedDrawings { get; }
    DbSet<DrawingRevision> DrawingRevisions { get; }
    DbSet<ExtractedFeature> ExtractedFeatures { get; }
    DbSet<Clash> Clashes { get; }
    DbSet<ReferenceLayer> ReferenceLayers { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<WorkspaceUserRole> WorkspaceUserRoles { get; }
    DbSet<DrawingStage> DrawingStages { get; }
    DbSet<ToleranceRule> ToleranceRules { get; }
    DbSet<FeatureMatch> FeatureMatches { get; }
    DbSet<ComplianceResult> ComplianceResults { get; }
    DbSet<Deviation> Deviations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IFeatureMatchingService
{
    Task<List<FeatureMatch>> MatchFeaturesAsync(Guid workspaceId, Guid asBuiltDrawingId, Guid gfcDrawingId, double searchDistanceM = 5.0, CancellationToken ct = default);
}

public interface IComplianceEngineService
{
    Task<List<ComplianceResult>> EvaluateComplianceAsync(Guid workspaceId, Guid asBuiltDrawingId, CancellationToken ct = default);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    Guid? WorkspaceId { get; }
}

public interface IFileStorage
{
    Task<string> SaveFileAsync(Stream stream, string fileName, string subFolder, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
    string GetFullPath(string relativePath);
}

public interface IDwgConverter
{
    Task<string> ConvertDwgToDxfAsync(string dwgPath, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task LogAsync(Guid workspaceId, string action, string entityName, string entityId, string? details, string? userId = null, CancellationToken cancellationToken = default);
}

public interface IReferenceLayerService
{
    Task<string> ParseToGeoJsonAsync(Stream fileStream, string fileName, int sourceEpsg, CancellationToken cancellationToken = default);
}

