namespace Portal.Infrastructure.Screens;

/// <summary>
/// One field on a screen. <see cref="OptionsJson"/> is a schema addition made
/// during Phase 3 implementation — the master spec named Select/MultiSelect as
/// control types but never specified how their options are supplied; a nullable
/// JSON array of {value,label} pairs is the minimal addition that makes them
/// actually renderable. See docs/07-dynamic-screen-engine.md §2 and
/// docs/05-database-design.md §2.
/// </summary>
public sealed class ScreenField
{
    public Guid Id { get; set; }
    public Guid ScreenId { get; set; }
    public Screen Screen { get; set; } = null!;

    public required string FieldKey { get; set; }
    public required string Label { get; set; }
    public required string DataType { get; set; }
    public required string ControlType { get; set; }
    public bool Required { get; set; }
    public bool Editable { get; set; } = true;
    public bool Visible { get; set; } = true;
    public int DisplayOrder { get; set; }

    /// <summary>Permission required to see this field at all — absent fields are omitted from the API response, not just disabled.</summary>
    public string? Permission { get; set; }

    /// <summary>Maps this field to the backend command/query property that actually reads/writes it.</summary>
    public required string IntegrationKey { get; set; }

    /// <summary>JSON array of {"value": "...", "label": "..."} — only meaningful for Select/MultiSelect.</summary>
    public string? OptionsJson { get; set; }
}
