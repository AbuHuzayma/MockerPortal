using Microsoft.EntityFrameworkCore;
using Portal.Application.Screens;
using Portal.Infrastructure.Persistence;
using Portal.Infrastructure.Screens;
using Xunit;

namespace Portal.IntegrationTests.Screens;

public class ScreenServiceTests
{
    private static PortalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase($"ScreenServiceTests-{Guid.NewGuid()}")
            .Options;
        return new PortalDbContext(options);
    }

    private static async Task<PortalDbContext> SeedScreenAsync()
    {
        var dbContext = CreateContext();
        var screen = new Screen { Id = Guid.NewGuid(), Code = "TEST_SCREEN", Name = "Test Screen", Category = "Test" };
        dbContext.Screens.Add(screen);
        dbContext.ScreenFields.AddRange(
            new ScreenField { Id = Guid.NewGuid(), ScreenId = screen.Id, FieldKey = "open", Label = "Open", DataType = "Text", ControlType = "Text", IntegrationKey = "open", DisplayOrder = 0 },
            new ScreenField { Id = Guid.NewGuid(), ScreenId = screen.Id, FieldKey = "gated", Label = "Gated", DataType = "Text", ControlType = "Text", IntegrationKey = "gated", DisplayOrder = 1, Permission = "admin.users" },
            new ScreenField { Id = Guid.NewGuid(), ScreenId = screen.Id, FieldKey = "hidden", Label = "Hidden", DataType = "Text", ControlType = "Text", IntegrationKey = "hidden", DisplayOrder = 2, Visible = false });
        dbContext.ScreenActions.Add(
            new ScreenAction { Id = Guid.NewGuid(), ScreenId = screen.Id, Code = "SAVE", Label = "Save", Permission = "admin.users" });
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    [Fact]
    public async Task GetScreenDefinitionAsync_omits_fields_and_actions_the_caller_lacks_permission_for()
    {
        var dbContext = await SeedScreenAsync();
        var service = new ScreenService(dbContext);

        var result = await service.GetScreenDefinitionAsync("TEST_SCREEN", new HashSet<string>(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var fieldKeys = result.Value!.Fields.Select(f => f.FieldKey).ToList();
        Assert.Contains("open", fieldKeys);
        Assert.DoesNotContain("gated", fieldKeys);
        Assert.DoesNotContain("hidden", fieldKeys); // Visible = false, regardless of permission
        Assert.Empty(result.Value.Actions);
    }

    [Fact]
    public async Task GetScreenDefinitionAsync_includes_gated_field_and_action_when_caller_has_the_permission()
    {
        var dbContext = await SeedScreenAsync();
        var service = new ScreenService(dbContext);

        var result = await service.GetScreenDefinitionAsync("TEST_SCREEN", new HashSet<string> { "admin.users" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Fields, f => f.FieldKey == "gated");
        Assert.Contains(result.Value.Actions, a => a.Code == "SAVE");
    }

    [Fact]
    public async Task GetScreenDefinitionAsync_returns_not_found_for_an_unknown_code()
    {
        var dbContext = CreateContext();
        var service = new ScreenService(dbContext);

        var result = await service.GetScreenDefinitionAsync("DOES_NOT_EXIST", new HashSet<string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScreenErrorCodes.ScreenNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetScreenDefinitionAsync_returns_forbidden_when_caller_lacks_a_screen_level_permission()
    {
        var dbContext = CreateContext();
        var screen = new Screen { Id = Guid.NewGuid(), Code = "GATED_SCREEN", Name = "Gated", Category = "Test" };
        dbContext.Screens.Add(screen);
        dbContext.ScreenPermissions.Add(new ScreenPermission { Id = Guid.NewGuid(), ScreenId = screen.Id, Permission = "admin.users" });
        await dbContext.SaveChangesAsync();
        var service = new ScreenService(dbContext);

        var result = await service.GetScreenDefinitionAsync("GATED_SCREEN", new HashSet<string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScreenErrorCodes.ScreenForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task ScreenSeeder_seeds_the_sample_screen_idempotently()
    {
        var dbContext = CreateContext();

        await ScreenSeeder.SeedSampleScreenAsync(dbContext);
        await ScreenSeeder.SeedSampleScreenAsync(dbContext); // second run must not duplicate

        var screens = await dbContext.Screens.Where(s => s.Code == ScreenCodes.SampleScreen).ToListAsync();
        Assert.Single(screens);

        var fields = await dbContext.ScreenFields.Where(f => f.ScreenId == screens[0].Id).ToListAsync();
        Assert.Equal(9, fields.Count);
    }
}
