using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class MerchantsFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task Search_without_authentication_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/merchants/search?name=Falcon");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_with_a_known_name_returns_the_merchant()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/merchants/search?name=Falcon");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<List<Dictionary<string, object?>>>>();
        Assert.Single(body!.Data!);
    }

    [Fact]
    public async Task Search_with_an_unknown_name_returns_not_found()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/merchants/search?name=DoesNotExist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("MERCHANT_NOT_FOUND", body!.Error!.Code);
    }

    [Fact]
    public async Task ReadOnly_user_cannot_update_a_merchant()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/merchants/MERCH-200001", new Dictionary<string, object?> { ["nameEn"] = "X" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_update_a_merchant_and_the_change_persists()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var saveResponse = await client.PutAsJsonAsync("/api/v1/merchants/MERCH-200002", new Dictionary<string, object?> { ["brandNameEn"] = "New Brand" });

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        var saveBody = await saveResponse.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("New Brand", saveBody!.Data!["brandNameEn"]?.ToString());

        var afterGet = await (await client.GetAsync("/api/v1/merchants/MERCH-200002"))
            .Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("New Brand", afterGet!.Data!["brandNameEn"]?.ToString());
    }

    [Fact]
    public async Task B2B_add_moves_status_from_not_subscribed_to_subscribed()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var before = await (await client.GetAsync("/api/v1/merchants/MERCH-200001/b2b"))
            .Content.ReadFromJsonAsync<DataEnvelope<B2BStatusDto>>();
        Assert.Equal("NotSubscribed", before!.Data!.Status);

        var addResponse = await client.PostAsync("/api/v1/merchants/MERCH-200001/b2b", null);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var after = await addResponse.Content.ReadFromJsonAsync<DataEnvelope<B2BStatusDto>>();
        Assert.Equal("Subscribed", after!.Data!.Status);
    }

    private sealed record B2BStatusDto(string MerchantId, string Status);
}
