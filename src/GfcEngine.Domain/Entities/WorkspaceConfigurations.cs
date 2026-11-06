using GfcEngine.Domain.Common;

namespace GfcEngine.Domain.Entities;

public class CadLayerMapping : WorkspaceScopedEntity
{
    public string CadLayer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string GeometryType { get; set; } = string.Empty; // "line", "point", "polygon"
    public string Color { get; set; } = "#000000";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace? Workspace { get; set; }
}

public class CategoryConfig : WorkspaceScopedEntity
{
    public string CategoryName { get; set; } = string.Empty;
    public string SubTypeName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#1f77ff";
    public string IconName { get; set; } = "layers";
    public int DisplayOrder { get; set; }

    public Workspace? Workspace { get; set; }
}

public class CategoryFieldConfig : WorkspaceScopedEntity
{
    public string CategoryName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text"; // "text", "number", "date", "select"
    public bool IsRequired { get; set; }
    public string? OptionsJson { get; set; } // for select options
    public int DisplayOrder { get; set; }

    public Workspace? Workspace { get; set; }
}

