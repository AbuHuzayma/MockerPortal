namespace Portal.Infrastructure.Screens;

public sealed class ScreenAction
{
    public Guid Id { get; set; }
    public Guid ScreenId { get; set; }
    public Screen Screen { get; set; } = null!;

    public required string Code { get; set; }
    public required string Label { get; set; }
    public string? Permission { get; set; }
    public bool RequiresConfirmation { get; set; }
}
