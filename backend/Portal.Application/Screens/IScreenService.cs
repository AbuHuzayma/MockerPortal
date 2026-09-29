using Portal.Application.Common;

namespace Portal.Application.Screens;

/// <summary>
/// Reads dynamic screen metadata, filtered to the caller's effective permissions
/// — see docs/07-dynamic-screen-engine.md §2. This governs presentation only;
/// the write path for any real screen is always a separate, reviewed backend
/// command (CLAUDE.md §11), never something this service can do.
/// </summary>
public interface IScreenService
{
    Task<Result<ScreenDefinitionDto>> GetScreenDefinitionAsync(
        string code,
        IReadOnlySet<string> callerPermissions,
        CancellationToken ct);

    /// <summary>Unfiltered, for the admin screen viewer (admin.screens) only — never for
    /// the normal screen-rendering path, which must stay permission-filtered.</summary>
    Task<IReadOnlyList<ScreenAdminDto>> ListAllAsync(CancellationToken ct);
}
