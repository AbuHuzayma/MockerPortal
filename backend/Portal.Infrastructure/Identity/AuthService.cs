using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Portal.Application.Auth;
using Portal.Application.Common;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    PortalDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    IEnvironmentContext environmentContext,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<Result<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            // Same error for "no such user" and "wrong password" — avoids confirming
            // account existence to an unauthenticated caller.
            return Result<AuthTokens>.Failure(AuthErrorCodes.InvalidCredentials, "Invalid email or password.");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            logger.LogWarning("Login blocked: account {UserId} is locked out.", user.Id);
            return Result<AuthTokens>.Failure(AuthErrorCodes.AccountLocked, "Account is temporarily locked due to repeated failed attempts.");
        }

        if (!signInResult.Succeeded)
        {
            return Result<AuthTokens>.Failure(AuthErrorCodes.InvalidCredentials, "Invalid email or password.");
        }

        var tokens = await IssueTokensAsync(user, ct);
        return Result<AuthTokens>.Success(tokens);
    }

    public async Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var tokenHash = Hash(refreshToken);
        var existing = await dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (existing is null || !existing.IsActive)
        {
            return Result<AuthTokens>.Failure(AuthErrorCodes.RefreshTokenInvalid, "Refresh token is invalid or expired.");
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result<AuthTokens>.Failure(AuthErrorCodes.RefreshTokenInvalid, "Refresh token is invalid or expired.");
        }

        var tokens = await IssueTokensAsync(user, ct);

        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.ReplacedByTokenHash = Hash(tokens.RefreshToken);
        await dbContext.SaveChangesAsync(ct);

        return Result<AuthTokens>.Success(tokens);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var tokenHash = Hash(refreshToken);
        var existing = await dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (existing is not null && existing.RevokedAtUtc is null)
        {
            existing.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await GetEffectivePermissionsAsync(roles, ct);

        return new CurrentUserDto(user.Id, user.Email!, user.FullName, [.. roles], permissions, environmentContext.Name);
    }

    private async Task<AuthTokens> IssueTokensAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await GetEffectivePermissionsAsync(roles, ct);

        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenLifetimeMinutes);
        var accessToken = GenerateAccessToken(user, roles, permissions, accessTokenExpiresAt);

        var refreshTokenRaw = GenerateRefreshTokenValue();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenLifetimeDays);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(refreshTokenRaw),
            ExpiresAtUtc = refreshTokenExpiresAt
        });
        await dbContext.SaveChangesAsync(ct);

        return new AuthTokens(accessToken, accessTokenExpiresAt, refreshTokenRaw, refreshTokenExpiresAt);
    }

    private string GenerateAccessToken(ApplicationUser user, IList<string> roles, IReadOnlyList<string> permissions, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(_jwt.SigningKey))
        {
            throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("name", user.FullName),
            new("env", environmentContext.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim(AppClaimTypes.Permission, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(IList<string> roleNames, CancellationToken ct)
    {
        if (roleNames.Count == 0)
        {
            return [];
        }

        var roleIds = await dbContext.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct);

        return await dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(ct);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateRefreshTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
