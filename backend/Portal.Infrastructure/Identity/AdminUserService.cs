using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Admin;
using Portal.Application.Common;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Identity;

public sealed class AdminUserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    PortalDbContext dbContext) : IAdminUserService
{
    public async Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(CancellationToken ct)
    {
        var users = await dbContext.Users.OrderBy(u => u.Email).ToListAsync(ct);
        var result = new List<UserSummaryDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(ToDto(user, roles));
        }
        return result;
    }

    public async Task<Result<UserSummaryDto>> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        if (!await roleManager.RoleExistsAsync(request.Role))
        {
            return Result<UserSummaryDto>.Failure(AdminErrorCodes.RoleNotFound, $"Role \"{request.Role}\" does not exist.");
        }

        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Result<UserSummaryDto>.Failure(AdminErrorCodes.EmailAlreadyInUse, "A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result<UserSummaryDto>.Failure(
                AdminErrorCodes.CreateUserFailed,
                string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, request.Role);
        return Result<UserSummaryDto>.Success(ToDto(user, [request.Role]));
    }

    public async Task<Result<UserSummaryDto>> SetRoleAsync(Guid userId, string role, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserSummaryDto>.Failure(AdminErrorCodes.UserNotFound, "User was not found.");
        }

        if (!await roleManager.RoleExistsAsync(role))
        {
            return Result<UserSummaryDto>.Failure(AdminErrorCodes.RoleNotFound, $"Role \"{role}\" does not exist.");
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        await userManager.AddToRoleAsync(user, role);

        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return Result<UserSummaryDto>.Success(ToDto(user, [role]));
    }

    public async Task<Result<UserSummaryDto>> SetActiveAsync(Guid userId, bool isActive, Guid callerUserId, CancellationToken ct)
    {
        if (!isActive && userId == callerUserId)
        {
            return Result<UserSummaryDto>.Failure(AdminErrorCodes.CannotDisableSelf, "You cannot disable your own account.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserSummaryDto>.Failure(AdminErrorCodes.UserNotFound, "User was not found.");
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        return Result<UserSummaryDto>.Success(ToDto(user, roles));
    }

    public async Task<Result<bool>> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<bool>.Failure(AdminErrorCodes.UserNotFound, "User was not found.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return Result<bool>.Failure(
                AdminErrorCodes.ResetPasswordFailed,
                string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return Result<bool>.Success(true);
    }

    public async Task<IReadOnlyList<RoleSummaryDto>> ListRolesAsync(CancellationToken ct)
    {
        var roles = await dbContext.Roles.OrderBy(r => r.Name).ToListAsync(ct);
        var result = new List<RoleSummaryDto>(roles.Count);
        foreach (var role in roles)
        {
            var permissionCodes = await dbContext.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.Permission.Code)
                .OrderBy(code => code)
                .ToListAsync(ct);
            result.Add(new RoleSummaryDto(role.Name!, role.Description, permissionCodes));
        }
        return result;
    }

    public IReadOnlyList<PermissionDto> ListPermissionCatalog() =>
        PermissionCatalog.All.Select(p => new PermissionDto(p.Code, p.Description, p.Category)).ToList();

    private static UserSummaryDto ToDto(ApplicationUser user, IList<string> roles) => new(
        user.Id, user.Email!, user.FullName, [.. roles], user.IsActive, user.CreatedAt);
}
