using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Portal.Infrastructure.Persistence;

/// <summary>
/// Used only by the `dotnet ef migrations add` design-time tooling — never at
/// runtime (the app wires PortalDbContext via DependencyInjection.AddInfrastructure
/// using the real, environment-specific connection string). A placeholder
/// connection string here is fine: generating a migration never opens a connection.
/// </summary>
public sealed class PortalDbContextFactory : IDesignTimeDbContextFactory<PortalDbContext>
{
    public PortalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PortalDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=portal;Username=portal;Password=portal_dev_password");

        return new PortalDbContext(optionsBuilder.Options);
    }
}
