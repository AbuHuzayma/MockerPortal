using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class CustomersFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task Search_without_authentication_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customers/search?mobileNumber=0500000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_with_a_known_mobile_number_returns_the_customer()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.GetAsync("/api/v1/customers/search?mobileNumber=0500000001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<CustomerData>>();
        Assert.Equal("CUST-100001", body!.Data!.CustId);
    }

    [Fact]
    public async Task Search_with_an_unknown_mobile_number_returns_customer_not_found()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.GetAsync("/api/v1/customers/search?mobileNumber=0599999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("CUSTOMER_NOT_FOUND", body!.Error!.Code);
    }

    [Fact]
    public async Task Search_with_an_invalid_mobile_number_returns_validation_error()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.GetAsync("/api/v1/customers/search?mobileNumber=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("VALIDATION_FAILED", body!.Error!.Code);
    }

    [Fact]
    public async Task GetProfile_with_a_known_id_returns_the_customer()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-100002");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<CustomerData>>();
        Assert.Equal("0500000002", body!.Data!.MobileNo);
    }

    [Fact]
    public async Task GetProfile_with_an_unknown_id_returns_customer_not_found()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-DOES-NOT-EXIST");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("CUSTOMER_NOT_FOUND", body!.Error!.Code);
    }

    private sealed record CustomerData(string CustId, string CustNumber, string MobileNo);
}
