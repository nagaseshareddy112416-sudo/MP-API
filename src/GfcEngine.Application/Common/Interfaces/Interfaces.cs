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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
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

