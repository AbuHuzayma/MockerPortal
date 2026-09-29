namespace Portal.Infrastructure.Screens;

/// <summary>EF entity for dynamic screen metadata — see docs/05-database-design.md §2 and docs/07.</summary>
public sealed class Screen
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<ScreenField> Fields { get; set; } = [];
    public ICollection<ScreenAction> Actions { get; set; } = [];
    public ICollection<ScreenPermission> Permissions { get; set; } = [];
}
