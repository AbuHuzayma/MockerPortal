using Microsoft.EntityFrameworkCore;
using Portal.Application.Common;
using Portal.Application.Mocking;
using Portal.Infrastructure.Mocking;
using Portal.Infrastructure.Persistence;
using Xunit;

namespace Portal.IntegrationTests.Mocking;

public class MockAdminServiceTests
{
    private static PortalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase($"MockAdminServiceTests-{Guid.NewGuid()}")
            .Options;
        return new PortalDbContext(options);
    }

    private sealed class StaticEnvironmentContext(string name) : IEnvironmentContext
    {
        public string Name { get; } = name;
        public EnvironmentInfo ToPublicInfo() => new(Name, Name, "#000000");
    }

    private static MockApiDto NewDto(string code) => new(
        null, code, "Test", "desc", "irrelevant-overridden-by-server", true, []);

    [Fact]
    public async Task GetAsync_and_SetActiveAsync_do_not_throw_when_the_same_code_exists_in_another_environment()
    {
        // Regression: a shared dev database can hold the same MockApi code seeded
        // into DEV/QA/PREPROD (see MockDataSeeder). Every admin operation must stay
        // scoped to the current environment or SingleOrDefaultAsync throws.
        var dbContext = CreateContext();
        var devService = new MockAdminService(dbContext, new StaticEnvironmentContext("DEV"));
        var qaService = new MockAdminService(dbContext, new StaticEnvironmentContext("QA"));

        await devService.UpsertAsync(NewDto("SHARED"), CancellationToken.None);
        await qaService.UpsertAsync(NewDto("SHARED"), CancellationToken.None);

        var getResult = await devService.GetAsync("SHARED", CancellationToken.None);
        Assert.True(getResult.IsSuccess);
        Assert.Equal("DEV", getResult.Value!.Environment);

        var setActiveResult = await devService.SetActiveAsync("SHARED", false, CancellationToken.None);
        Assert.True(setActiveResult.IsSuccess);
        Assert.False(setActiveResult.Value!.IsActive);

        // The QA row must be untouched by the DEV-scoped operation.
        var qaGet = await qaService.GetAsync("SHARED", CancellationToken.None);
        Assert.True(qaGet.Value!.IsActive);
    }

    [Fact]
    public async Task UpsertAsync_ignores_a_client_supplied_environment_and_uses_the_deployed_environment()
    {
        var dbContext = CreateContext();
        var qaService = new MockAdminService(dbContext, new StaticEnvironmentContext("QA"));

        var saved = await qaService.UpsertAsync(NewDto("IGNORED-ENV"), CancellationToken.None);

        Assert.Equal("QA", saved.Environment);
    }

    [Fact]
    public async Task ListAsync_only_returns_MockApis_for_the_current_environment()
    {
        var dbContext = CreateContext();
        var devService = new MockAdminService(dbContext, new StaticEnvironmentContext("DEV"));
        var qaService = new MockAdminService(dbContext, new StaticEnvironmentContext("QA"));

        await devService.UpsertAsync(NewDto("DEV-ONLY"), CancellationToken.None);
        await qaService.UpsertAsync(NewDto("QA-ONLY"), CancellationToken.None);

        var devList = await devService.ListAsync(CancellationToken.None);

        Assert.Contains(devList, a => a.Code == "DEV-ONLY");
        Assert.DoesNotContain(devList, a => a.Code == "QA-ONLY");
    }
}
