using Microsoft.AspNetCore.Authorization;
using Portal.Application.Common;

namespace Portal.Api.Authorization;

public sealed class PermissionAuthorizationHandler(IEnvironmentContext environmentContext)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToHashSet();

        if (!permissions.Contains(requirement.PermissionCode))
        {
            return Task.CompletedTask;
        }

        // Environment-scoped permissions only ever exist as .qa/.preprod variants
        // (docs/04-authentication-authorization.md §2) — there is no .dev permission
        // code, so DEV is intentionally exempt from the extra check. Without this,
        // no environment-scoped action could ever succeed locally.
        //
        // The extra check is only enforced when a "{code}.{env}" permission is
        // actually DEFINED in the catalog for the current environment. Some
        // operations (customer.kyc.update) define both .qa and .preprod variants;
        // others (customer.security.remove, api-mocker.enable) define only
        // .preprod, precisely so they're never extra-gated in QA — api-mocker.enable
        // in particular needs to work in QA, since that's the mocker's main use case.
        var isDev = string.Equals(environmentContext.Name, "DEV", StringComparison.OrdinalIgnoreCase);
        if (requirement.EnvironmentScoped && !isDev)
        {
            var scopedCode = $"{requirement.PermissionCode}.{environmentContext.Name.ToLowerInvariant()}";
            var scopedPermissionIsDefined = PermissionCatalog.All.Any(p => p.Code == scopedCode);
            if (scopedPermissionIsDefined && !permissions.Contains(scopedCode))
            {
                return Task.CompletedTask;
            }
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
