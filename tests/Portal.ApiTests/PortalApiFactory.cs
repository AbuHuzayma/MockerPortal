using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portal.ApiTests;

/// <summary>
/// Runs the real Portal.Api pipeline (auth, authorization, controllers, middleware)
/// against an EF Core InMemory database instead of Postgres, via the
/// Database:UseInMemoryProvider config flag (Portal.Infrastructure.DependencyInjection)
/// — a deliberate, documented simplification (docs/14-testing-strategy.md) so the
/// API test suite doesn't depend on Docker/Testcontainers being available.
///
/// Uses UseSetting (not ConfigureAppConfiguration) because Program.cs reads
/// builder.Configuration eagerly — before WebApplicationFactory's ConfigureWebHost
/// hook would otherwise take effect — and UseSetting is the one override path
/// guaranteed to be visible to WebApplicationBuilder at that point.
/// </summary>
public sealed class PortalApiFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "api-tests-only-signing-key-never-used-outside-ci-0123456789";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:AutoMigrate", "false");
        builder.UseSetting("Database:UseInMemoryProvider", "true");
        builder.UseSetting("Database:InMemoryName", $"PortalApiTests-{Guid.NewGuid()}");
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
    }
}
