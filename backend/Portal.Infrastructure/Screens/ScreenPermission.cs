namespace Portal.Infrastructure.Screens;

/// <summary>Screen-level view gate: the user must hold every permission listed here to receive the screen definition at all.</summary>
public sealed class ScreenPermission
{
    public Guid Id { get; set; }
    public Guid ScreenId { get; set; }
    public Screen Screen { get; set; } = null!;

    public required string Permission { get; set; }
}
