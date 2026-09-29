namespace Portal.Application.Admin;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreateUserRequest(string Email, string FullName, string Password, string Role);

public sealed record RoleSummaryDto(string Name, string? Description, IReadOnlyList<string> PermissionCodes);

public sealed record PermissionDto(string Code, string Description, string Category);

public static class AdminErrorCodes
{
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string EmailAlreadyInUse = "EMAIL_ALREADY_IN_USE";
    public const string RoleNotFound = "ROLE_NOT_FOUND";
    public const string CreateUserFailed = "CREATE_USER_FAILED";
    public const string ResetPasswordFailed = "RESET_PASSWORD_FAILED";
    public const string CannotDisableSelf = "CANNOT_DISABLE_SELF";
}
