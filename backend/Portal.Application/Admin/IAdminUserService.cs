using Portal.Application.Common;

namespace Portal.Application.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(CancellationToken ct);

    Task<Result<UserSummaryDto>> CreateUserAsync(CreateUserRequest request, CancellationToken ct);

    Task<Result<UserSummaryDto>> SetRoleAsync(Guid userId, string role, CancellationToken ct);

    Task<Result<UserSummaryDto>> SetActiveAsync(Guid userId, bool isActive, Guid callerUserId, CancellationToken ct);

    Task<Result<bool>> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct);

    Task<IReadOnlyList<RoleSummaryDto>> ListRolesAsync(CancellationToken ct);

    IReadOnlyList<PermissionDto> ListPermissionCatalog();
}
