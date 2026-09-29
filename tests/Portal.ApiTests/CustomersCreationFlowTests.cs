using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

public class CustomersCreationFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task GetCreation_without_authentication_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customers/CUST-100001/creation");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCreation_returns_the_seeded_dates()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-100001/creation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("2022-03-10", body!.Data!["createdDate"]?.ToString());
    }

    [Fact]
    public async Task GetCreation_for_an_unknown_customer_returns_not_found()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-DOES-NOT-EXIST/creation");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("CUSTOMER_NOT_FOUND", body!.Error!.Code);
    }

    [Fact]
    public async Task ReadOnly_user_cannot_update_creation_details()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/customers/CUST-100001/creation", new Dictionary<string, object?> { ["dateOfBirth"] = "1990-01-01" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_with_a_future_date_of_birth_returns_validation_error()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");
        var futureDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");

        var response = await client.PutAsJsonAsync("/api/v1/customers/CUST-100001/creation", new Dictionary<string, object?> { ["dateOfBirth"] = futureDate });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("VALIDATION_FAILED", body!.Error!.Code);
    }

    [Fact]
    public async Task QA_user_can_update_creation_details_and_the_change_persists()
    {
        var client = await factory.CreateAuthenticatedClientAsync("qa@portal.local");

        var saveResponse = await client.PutAsJsonAsync("/api/v1/customers/CUST-100003/creation", new Dictionary<string, object?> { ["kycDate"] = "2024-06-01" });

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        var saveBody = await saveResponse.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("2024-06-01", saveBody!.Data!["kycDate"]?.ToString());

        var afterGet = await (await client.GetAsync("/api/v1/customers/CUST-100003/creation"))
            .Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("2024-06-01", afterGet!.Data!["kycDate"]?.ToString());
    }
}
