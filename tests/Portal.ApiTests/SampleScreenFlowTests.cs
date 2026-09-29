using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class SampleScreenFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task GetRecord_returns_the_seeded_sample_record()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.GetAsync("/api/v1/sample-screen/record");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<SampleRecord>>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Data!.RecordId));
    }

    [Fact]
    public async Task Save_with_invalid_status_returns_validation_error()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");
        var current = await (await client.GetAsync("/api/v1/sample-screen/record"))
            .Content.ReadFromJsonAsync<DataEnvelope<SampleRecord>>();

        var response = await client.PostAsJsonAsync("/api/v1/sample-screen/save", current!.Data! with { Status = "NotARealStatus" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("VALIDATION_FAILED", body!.Error!.Code);
    }

    [Fact]
    public async Task Save_persists_the_change_and_is_reflected_on_the_next_get()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");
        var current = await (await client.GetAsync("/api/v1/sample-screen/record"))
            .Content.ReadFromJsonAsync<DataEnvelope<SampleRecord>>();

        var updated = current!.Data! with { FullName = "Updated By Test", Notes = "changed" };
        var saveResponse = await client.PostAsJsonAsync("/api/v1/sample-screen/save", updated);

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        var saveBody = await saveResponse.Content.ReadFromJsonAsync<DataEnvelope<SampleRecord>>();
        Assert.Equal("Updated By Test", saveBody!.Data!.FullName);

        var afterGet = await (await client.GetAsync("/api/v1/sample-screen/record"))
            .Content.ReadFromJsonAsync<DataEnvelope<SampleRecord>>();
        Assert.Equal("Updated By Test", afterGet!.Data!.FullName);
    }

    private sealed record SampleRecord(
        string RecordId,
        string FullName,
        int? Age,
        decimal? Balance,
        DateOnly? JoinDate,
        bool IsActive,
        string Status,
        string? Notes,
        string? InternalNote);
}
