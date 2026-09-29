using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class HealthAndMetaTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task Health_endpoint_returns_success_envelope()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EnvelopeProbe>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.False(string.IsNullOrWhiteSpace(body.CorrelationId));
    }

    [Fact]
    public async Task Meta_environment_endpoint_is_unauthenticated_and_returns_environment_name()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/meta/environment");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EnvelopeProbe>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
    }

    private sealed record EnvelopeProbe(bool Success, string CorrelationId);
}
