using System.Security.Claims;
using GfcEngine.Application.Common.Interfaces;

namespace GfcEngine.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public Guid? WorkspaceId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Request.Headers.TryGetValue("X-Workspace-Id", out var headerVal) &&
                Guid.TryParse(headerVal, out var wsId))
            {
                return wsId;
            }

            if (context.Request.Query.TryGetValue("workspaceId", out var queryVal) &&
                Guid.TryParse(queryVal, out var queryWsId))
            {
                return queryWsId;
            }

            return null;
        }
    }
}
