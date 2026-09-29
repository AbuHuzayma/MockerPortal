using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class CustomersIvrFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task GetIvr_without_authentication_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customers/CUST-100001/ivr");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetIvr_returns_an_empty_field_set_for_a_known_customer()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-100001/ivr");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("CUST-100001", body!.Data!["custId"]?.ToString());
    }

    [Fact]
    public async Task GetIvr_for_an_unknown_customer_returns_not_found()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-DOES-NOT-EXIST/ivr");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("CUSTOMER_NOT_FOUND", body!.Error!.Code);
    }

    [Fact]
    public async Task ReadOnly_user_cannot_update_ivr()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/customers/CUST-100001/ivr", new Dictionary<string, object?> { ["anything"] = "value" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task QA_user_can_call_update_but_every_field_is_dropped_since_the_catalog_is_empty()
    {
        var client = await factory.CreateAuthenticatedClientAsync("qa@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/customers/CUST-100002/ivr",
            new Dictionary<string, object?> { ["somethingMadeUp"] = "should never persist" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.False(body!.Data!.ContainsKey("somethingMadeUp"));
    }
}
