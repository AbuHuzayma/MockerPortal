using Microsoft.AspNetCore.Authorization;

namespace Portal.Api.Authorization;

/// <summary>
/// Requires the caller to hold <see cref="PermissionCode"/>. When
/// <see cref="EnvironmentScoped"/> is true, the caller must additionally hold
/// "{PermissionCode}.{environment}" for the request's resolved environment —
/// see docs/04-authentication-authorization.md §2.
/// </summary>
public sealed class PermissionRequirement(string permissionCode, bool environmentScoped = false) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
    public bool EnvironmentScoped { get; } = environmentScoped;
}
