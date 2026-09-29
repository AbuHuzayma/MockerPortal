using Portal.Application.Common;

namespace Portal.Application.Mocking;

/// <summary>
/// Deliberately a "get/upsert whole tree by code" API rather than granular
/// per-level CRUD endpoints (docs/06's original sketch) — one atomic call per
/// MockApi tree is simpler to reason about and less prone to partial-update
/// bugs than a dozen nested endpoints, at the cost of always replacing a
/// MockApi's full endpoint/response/match-rule set on each save. Documented
/// deviation — see docs/09-api-mocker.md.
/// </summary>
public interface IMockAdminService
{
    Task<IReadOnlyList<MockApiDto>> ListAsync(CancellationToken ct);

    Task<Result<MockApiDto>> GetAsync(string code, CancellationToken ct);

    Task<MockApiDto> UpsertAsync(MockApiDto api, CancellationToken ct);

    Task<Result<bool>> DeleteAsync(string code, CancellationToken ct);

    Task<Result<MockApiDto>> SetActiveAsync(string code, bool isActive, CancellationToken ct);
}
