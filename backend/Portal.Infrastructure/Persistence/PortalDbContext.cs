using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portal.Infrastructure.Audit;
using Portal.Infrastructure.Identity;
using Portal.Infrastructure.Mocking;
using Portal.Infrastructure.Screens;

namespace Portal.Infrastructure.Persistence;

/// <summary>
/// The portal's own PostgreSQL database — users/roles/permissions/audit/mock config.
/// Never used to store real enterprise customer/merchant data. See docs/05-database-design.md §2.
/// </summary>
public sealed class PortalDbContext(DbContextOptions<PortalDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<Screen> Screens => Set<Screen>();
    public DbSet<ScreenField> ScreenFields => Set<ScreenField>();
    public DbSet<ScreenAction> ScreenActions => Set<ScreenAction>();
    public DbSet<ScreenPermission> ScreenPermissions => Set<ScreenPermission>();
    public DbSet<MockApi> MockApis => Set<MockApi>();
    public DbSet<MockEndpoint> MockEndpoints => Set<MockEndpoint>();
    public DbSet<MockResponse> MockResponses => Set<MockResponse>();
    public DbSet<MockMatchRule> MockMatchRules => Set<MockMatchRule>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(r => r.Description).HasMaxLength(500);
        });

        builder.Entity<IdentityUserRole<Guid>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<Guid>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<Guid>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<IdentityUserToken<Guid>>(entity => entity.ToTable("UserTokens"));
        builder.Entity<IdentityRoleClaim<Guid>>(entity => entity.ToTable("RoleClaims"));

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).HasMaxLength(100).IsRequired();
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Description).HasMaxLength(500).IsRequired();
            entity.Property(p => p.Category).HasMaxLength(100).IsRequired();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity.HasOne(rp => rp.Role)
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).HasMaxLength(256).IsRequired();
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => rt.UserId);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Username).HasMaxLength(256).IsRequired();
            entity.Property(a => a.Environment).HasMaxLength(20).IsRequired();
            entity.Property(a => a.CustomerId).HasMaxLength(100);
            entity.Property(a => a.MerchantId).HasMaxLength(100);
            entity.Property(a => a.Screen).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Operation).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Entity).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Field).HasMaxLength(100);
            entity.Property(a => a.Result).HasMaxLength(20).IsRequired();
            entity.Property(a => a.CorrelationId).HasMaxLength(64).IsRequired();
            // Append-only: no update/delete path is exposed anywhere in the API.
            entity.HasIndex(a => a.CorrelationId);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Timestamp);
        });

        builder.Entity<OperationLog>(entity =>
        {
            entity.ToTable("OperationLogs");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(o => o.CommandName).HasMaxLength(150).IsRequired();
            entity.Property(o => o.Result).HasMaxLength(20).IsRequired();
            entity.HasIndex(o => o.CorrelationId);
            entity.HasIndex(o => o.Timestamp);
        });

        builder.Entity<Screen>(entity =>
        {
            entity.ToTable("Screens");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Code).HasMaxLength(100).IsRequired();
            entity.HasIndex(s => s.Code).IsUnique();
            entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Category).HasMaxLength(100).IsRequired();
        });

        builder.Entity<ScreenField>(entity =>
        {
            entity.ToTable("ScreenFields");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.FieldKey).HasMaxLength(100).IsRequired();
            entity.Property(f => f.Label).HasMaxLength(200).IsRequired();
            entity.Property(f => f.DataType).HasMaxLength(50).IsRequired();
            entity.Property(f => f.ControlType).HasMaxLength(50).IsRequired();
            entity.Property(f => f.Permission).HasMaxLength(100);
            entity.Property(f => f.IntegrationKey).HasMaxLength(100).IsRequired();
            entity.HasOne(f => f.Screen)
                .WithMany(s => s.Fields)
                .HasForeignKey(f => f.ScreenId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(f => new { f.ScreenId, f.FieldKey }).IsUnique();
        });

        builder.Entity<ScreenAction>(entity =>
        {
            entity.ToTable("ScreenActions");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Code).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Label).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Permission).HasMaxLength(100);
            entity.HasOne(a => a.Screen)
                .WithMany(s => s.Actions)
                .HasForeignKey(a => a.ScreenId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(a => new { a.ScreenId, a.Code }).IsUnique();
        });

        builder.Entity<ScreenPermission>(entity =>
        {
            entity.ToTable("ScreenPermissions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Permission).HasMaxLength(100).IsRequired();
            entity.HasOne(p => p.Screen)
                .WithMany(s => s.Permissions)
                .HasForeignKey(p => p.ScreenId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MockApi>(entity =>
        {
            entity.ToTable("MockApis");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Code).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Name).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Environment).HasMaxLength(20).IsRequired();
            // Code is unique per environment, not globally — MockDataSeeder seeds the
            // same codes (ABSHER, YAKEEN, ELM) for DEV, QA and PREPROD.
            entity.HasIndex(a => new { a.Environment, a.Code }).IsUnique();
        });

        builder.Entity<MockEndpoint>(entity =>
        {
            entity.ToTable("MockEndpoints");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).HasMaxLength(500).IsRequired();
            entity.Property(e => e.HttpMethod).HasMaxLength(10).IsRequired();
            entity.HasOne(e => e.MockApi)
                .WithMany(a => a.Endpoints)
                .HasForeignKey(e => e.MockApiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MockResponse>(entity =>
        {
            entity.ToTable("MockResponses");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.Property(r => r.ResponseHeaders).HasColumnType("jsonb");
            entity.HasOne(r => r.MockEndpoint)
                .WithMany(e => e.Responses)
                .HasForeignKey(r => r.MockEndpointId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MockMatchRule>(entity =>
        {
            entity.ToTable("MockMatchRules");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Source).HasMaxLength(20).IsRequired();
            entity.Property(r => r.Field).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Operator).HasMaxLength(20).IsRequired();
            entity.Property(r => r.ExpectedValue).HasMaxLength(1000).IsRequired();
            entity.HasOne(r => r.MockResponse)
                .WithMany(resp => resp.MatchRules)
                .HasForeignKey(r => r.MockResponseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
