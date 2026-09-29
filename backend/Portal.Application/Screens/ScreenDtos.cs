namespace Portal.Application.Screens;

public sealed record ScreenFieldOptionDto(string Value, string Label);

public sealed record ScreenFieldDto(
    string FieldKey,
    string Label,
    string DataType,
    string ControlType,
    bool Required,
    bool Editable,
    int DisplayOrder,
    string IntegrationKey,
    IReadOnlyList<ScreenFieldOptionDto>? Options);

public sealed record ScreenActionDto(string Code, string Label, bool RequiresConfirmation);

public sealed record ScreenDefinitionDto(
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<ScreenFieldDto> Fields,
    IReadOnlyList<ScreenActionDto> Actions);

/// <summary>Unfiltered admin view of a screen's raw configuration — docs/07 §6: screens are seeded
/// from code, not admin-editable, so this is read-only (unlike API Mocker's data-driven config).</summary>
public sealed record ScreenAdminDto(
    string Code,
    string Name,
    string? Description,
    string Category,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<string> ScreenPermissions,
    IReadOnlyList<ScreenFieldDto> Fields,
    IReadOnlyList<ScreenActionDto> Actions);

public static class ScreenErrorCodes
{
    public const string ScreenNotFound = "SCREEN_NOT_FOUND";
    public const string ScreenForbidden = "SCREEN_FORBIDDEN";
}

public static class ScreenCodes
{
    /// <summary>The one Phase 3 proof-of-concept screen — see docs/07-dynamic-screen-engine.md §7.</summary>
    public const string SampleScreen = "SAMPLE_SCREEN";
}
