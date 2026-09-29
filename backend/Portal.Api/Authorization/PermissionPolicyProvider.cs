using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Portal.Api.Authorization;

/// <summary>
/// Builds an authorization policy on the fly for any policy name of the form
/// "Permission:{code}" or "Permission:{code}:env-scoped" — one per distinct
/// [HasPermission] usage, so permissions never need pre-registering as policies.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "Permission:";
    private const string EnvironmentScopedSuffix = ":env-scoped";

    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var remainder = policyName[PolicyPrefix.Length..];
        var environmentScoped = remainder.EndsWith(EnvironmentScopedSuffix, StringComparison.Ordinal);
        var code = environmentScoped ? remainder[..^EnvironmentScopedSuffix.Length] : remainder;

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(code, environmentScoped))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
