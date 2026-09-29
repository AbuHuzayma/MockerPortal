using Microsoft.AspNetCore.Authorization;

namespace Portal.Api.Authorization;

/// <summary>
/// Declares the permission required to reach an action. Every mutating/sensitive
/// endpoint carries one — see CLAUDE.md §8. Set <paramref name="environmentScoped"/>
/// for the operations docs/04-authentication-authorization.md §2 calls out
/// (KYC update, security lock removal, mock enable) so the caller must additionally
/// hold "{code}.{environment}" for the currently running environment.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string code, bool environmentScoped = false)
    {
        Policy = $"{PermissionPolicyProvider.PolicyPrefix}{code}{(environmentScoped ? ":env-scoped" : string.Empty)}";
    }
}
