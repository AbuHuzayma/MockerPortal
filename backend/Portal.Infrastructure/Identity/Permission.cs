namespace Portal.Infrastructure.Identity;

/// <summary>Row-per-permission table, seeded from Portal.Application.Common.PermissionCatalog.</summary>
public sealed class Permission
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
