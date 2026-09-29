using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class ScreensFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task Get_without_authentication_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/screens/SAMPLE_SCREEN");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_unknown_screen_code_returns_not_found()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/screens/DOES_NOT_EXIST");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("SCREEN_NOT_FOUND", body!.Error!.Code);
    }

    [Fact]
    public async Task ReadOnly_user_does_not_see_the_admin_only_field()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/screens/SAMPLE_SCREEN");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<ScreenDefinition>>();
        Assert.DoesNotContain(body!.Data!.Fields, f => f.FieldKey == "internalNote");
        Assert.Contains(body.Data.Fields, f => f.FieldKey == "fullName");
    }

    [Fact]
    public async Task Administrator_sees_the_admin_only_field()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.GetAsync("/api/v1/screens/SAMPLE_SCREEN");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<ScreenDefinition>>();
        Assert.Contains(body!.Data!.Fields, f => f.FieldKey == "internalNote");
    }

    [Fact]
    public async Task Select_field_carries_its_options()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.GetAsync("/api/v1/screens/SAMPLE_SCREEN");

        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<ScreenDefinition>>();
        var statusField = body!.Data!.Fields.Single(f => f.FieldKey == "status");
        Assert.Equal("Select", statusField.ControlType);
        Assert.NotNull(statusField.Options);
        Assert.Contains(statusField.Options!, o => o.Value == "Active");
    }

    private sealed record ScreenFieldOption(string Value, string Label);
    private sealed record ScreenField(string FieldKey, string ControlType, IReadOnlyList<ScreenFieldOption>? Options);
    private sealed record ScreenAction(string Code, bool RequiresConfirmation);
    private sealed record ScreenDefinition(string Code, string Name, IReadOnlyList<ScreenField> Fields, IReadOnlyList<ScreenAction> Actions);
}
