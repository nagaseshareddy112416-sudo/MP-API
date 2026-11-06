namespace GfcEngine.Application.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public List<WorkspaceRoleDto> Roles { get; set; } = new();
}

public class WorkspaceRoleDto
{
    public Guid WorkspaceId { get; set; }
    public string WorkspaceName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class WorkspaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DefaultEpsg { get; set; }
    public int DisplayEpsg { get; set; }
    public double CenterLon { get; set; }
    public double CenterLat { get; set; }
    public int DefaultZoom { get; set; }
    public double ClashVerticalToleranceM { get; set; }
    public double FarFromExistingDataWarningM { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateWorkspaceDto
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
}

public class CadLayerMappingDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string CadLayer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string GeometryType { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
}

public class CategoryConfigDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string SubTypeName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class CategoryFieldConfigDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text";
    public bool IsRequired { get; set; }
    public string? OptionsJson { get; set; }
    public int DisplayOrder { get; set; }
}
