namespace Portal.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

/// <summary>
/// Result of a successful login/refresh. The access token is returned to the caller
/// to hold in memory; the refresh token is set as an httpOnly cookie by the
/// controller and never appears in a JSON body. See docs/04-authentication-authorization.md §1.
/// </summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string Environment);

public static class AuthErrorCodes
{
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountDisabled = "ACCOUNT_DISABLED";
    public const string RefreshTokenInvalid = "REFRESH_TOKEN_INVALID";
}
