namespace GfcEngine.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public abstract class WorkspaceScopedEntity : BaseEntity
{
    public Guid WorkspaceId { get; set; }
}

