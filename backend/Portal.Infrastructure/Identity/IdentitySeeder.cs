using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portal.Application.Common;

namespace Portal.Infrastructure.Identity;

/// <summary>
/// Seed roles/permissions/role-permission mappings from the authoritative
/// <see cref="PermissionCatalog"/> (idempotent, runs in every environment), and —
/// Development only — the seed users from docs/04-authentication-authorization.md §2.
/// Never seeds real credentials; the dev password is a fixed, documented,
/// non-production value (see README.md).
/// </summary>
public static class IdentitySeeder
{
    public const string DevSeedPassword = "Dev-Only-Passw0rd!";

    private static readonly string[] ViewSuffixedOrExactView =
    [
        PermissionCodes.CustomerView,
        PermissionCodes.CustomerKycView,
        PermissionCodes.CustomerIvrView,
        PermissionCodes.CustomerCreationView,
        PermissionCodes.CustomerOtpView,
        PermissionCodes.CustomerCardView,
        PermissionCodes.CustomerBeneficiaryView,
        PermissionCodes.CustomerSecurityView,
        PermissionCodes.CustomerBiometricView,
        PermissionCodes.CustomerOnboardingView,
        PermissionCodes.MerchantView,
        PermissionCodes.MerchantB2BView,
        PermissionCodes.ApiMockerView,
        PermissionCodes.AuditView,
    ];

    private static readonly string[] QaExtraPermissions =
    [
        // Environment-scoped permissions are checked in ADDITION to the base code
        // (docs/04 §2), so the base code must be granted alongside the scoped one
        // or the scoped grant is inert.
        PermissionCodes.CustomerKycUpdate,
        PermissionCodes.CustomerKycUpdateQa,
        PermissionCodes.CustomerIvrUpdate,
        PermissionCodes.CustomerCreationUpdate,
        PermissionCodes.CustomerOtpUpdate,
        PermissionCodes.CustomerCardActivate,
        PermissionCodes.CustomerBeneficiaryActivate,
        PermissionCodes.CustomerBiometricUpdate,
        PermissionCodes.MerchantUpdate,
        PermissionCodes.MerchantB2BUpdate,
        PermissionCodes.ApiMockerManage,
        PermissionCodes.ApiMockerEnable,
        PermissionCodes.ApiMockerDisable,
    ];

    public static async Task SeedRolesAndPermissionsAsync(
        RoleManager<ApplicationRole> roleManager,
        Persistence.PortalDbContext dbContext,
        ILogger logger,
        CancellationToken ct = default)
    {
        foreach (var definition in PermissionCatalog.All)
        {
            var exists = await dbContext.Permissions.AnyAsync(p => p.Code == definition.Code, ct);
            if (!exists)
            {
                dbContext.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = definition.Code,
                    Description = definition.Description,
                    Category = definition.Category
                });
            }
        }
        await dbContext.SaveChangesAsync(ct);

        var administratorCodes = PermissionCatalog.All.Select(p => p.Code).ToArray();
        var qaCodes = ViewSuffixedOrExactView.Concat(QaExtraPermissions).Distinct().ToArray();
        var developerCodes = qaCodes; // see docs/04-authentication-authorization.md §2 note
        var readOnlyCodes = ViewSuffixedOrExactView;

        await EnsureRoleAsync(roleManager, dbContext, "Administrator", "Full access across all environments.", administratorCodes, logger, ct);
        await EnsureRoleAsync(roleManager, dbContext, "QA", "QA test-data operations, non-PREPROD-sensitive.", qaCodes, logger, ct);
        await EnsureRoleAsync(roleManager, dbContext, "Developer", "Same as QA plus broader mock management.", developerCodes, logger, ct);
        await EnsureRoleAsync(roleManager, dbContext, "ReadOnly", "View-only across all screens.", readOnlyCodes, logger, ct);
    }

    public static async Task SeedDevelopmentUsersAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken ct = default)
    {
        await EnsureUserAsync(userManager, "admin@portal.local", "Portal Administrator", "Administrator", logger, ct);
        await EnsureUserAsync(userManager, "qa@portal.local", "QA Tester", "QA", logger, ct);
        await EnsureUserAsync(userManager, "developer@portal.local", "Portal Developer", "Developer", logger, ct);
        await EnsureUserAsync(userManager, "readonly@portal.local", "Read Only User", "ReadOnly", logger, ct);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<ApplicationRole> roleManager,
        Persistence.PortalDbContext dbContext,
        string roleName,
        string description,
        IReadOnlyCollection<string> permissionCodes,
        ILogger logger,
        CancellationToken ct)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new ApplicationRole(roleName) { Description = description };
            var createResult = await roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create role {Role}: {Errors}", roleName,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        var permissionIds = await dbContext.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var existingPermissionIds = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        var toAdd = permissionIds.Except(existingPermissionIds);
        foreach (var permissionId in toAdd)
        {
            dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }

        var toRemove = existingPermissionIds.Except(permissionIds);
        var removeEntities = dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id && toRemove.Contains(rp.PermissionId));
        dbContext.RolePermissions.RemoveRange(removeEntities);

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string roleName,
        ILogger logger,
        CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
            };

            var createResult = await userManager.CreateAsync(user, DevSeedPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create dev user {Email}: {Errors}", email,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }
}
