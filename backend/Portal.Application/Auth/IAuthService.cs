using Portal.Application.Common;

namespace Portal.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken ct);

    /// <param name="refreshToken">The raw (unhashed) refresh token presented by the client.</param>
    Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct);

    Task LogoutAsync(string refreshToken, CancellationToken ct);

    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct);
}
