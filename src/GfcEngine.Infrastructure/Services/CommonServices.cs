using Microsoft.Extensions.Logging;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Domain.Entities;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.Infrastructure.Services;

public class StubDwgConverter : IDwgConverter
{
    private readonly ILogger<StubDwgConverter> _logger;

    public StubDwgConverter(ILogger<StubDwgConverter> logger)
    {
        _logger = logger;
    }

    public Task<string> ConvertDwgToDxfAsync(string dwgPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DWG to DXF conversion requested for: {DwgPath}", dwgPath);
        // In MVP, if the file is already DXF or stubbed, return DXF path or throw if real converter not present
        var dxfPath = Path.ChangeExtension(dwgPath, ".dxf");
        if (File.Exists(dxfPath))
        {
            return Task.FromResult(dxfPath);
        }

        throw new NotSupportedException("ODA File Converter is not installed in the environment. Please upload DXF directly for MVP processing.");
    }
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid workspaceId, string action, string entityName, string entityId, string? details, string? userId = null, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            WorkspaceId = workspaceId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            UserId = userId ?? "System",
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

