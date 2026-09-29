namespace Portal.Application.Auth;

/// <summary>
/// Bound from the "Jwt" configuration section. <see cref="SigningKey"/> is supplied
/// via environment variable / secret store only — never committed. See
/// docs/03-security.md §2 and docs/12-environments.md.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MockerPortal";
    public string Audience { get; set; } = "MockerPortal.Clients";
    public string? SigningKey { get; set; }
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
