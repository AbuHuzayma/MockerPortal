using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.Infrastructure.Persistence;
using Xunit;

namespace Portal.ApiTests;

public class CustomersKycFlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task GetKyc_without_authentication_is_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customers/CUST-100001/kyc");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetKyc_returns_the_kyc_record_with_a_computed_pep_flag()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-100001/kyc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("CUST-100001", body!.Data!["custId"]?.ToString());
        Assert.True(body.Data.ContainsKey("pep"));
    }

    [Fact]
    public async Task GetKyc_for_an_unknown_customer_returns_not_found()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-DOES-NOT-EXIST/kyc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("CUSTOMER_NOT_FOUND", body!.Error!.Code);
    }

    [Fact]
    public async Task ReadOnly_user_cannot_update_kyc()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/customers/CUST-100001/kyc", new Dictionary<string, object?> { ["firstName"] = "X" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task QA_user_can_update_kyc_in_the_dev_test_environment()
    {
        // Regression coverage for two Phase 1 fixes this phase surfaced:
        // (1) the QA role needs the base customer.kyc.update permission
        //     alongside the .qa-scoped one, and (2) DEV is exempt from the
        //     environment-scoped check (no .dev permission variant exists).
        var client = await factory.CreateAuthenticatedClientAsync("qa@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/customers/CUST-100002/kyc", new Dictionary<string, object?> { ["firstName"] = "QA Updated" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_with_a_malformed_email_returns_validation_error()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/customers/CUST-100001/kyc", new Dictionary<string, object?> { ["email"] = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("VALIDATION_FAILED", body!.Error!.Code);
    }

    [Fact]
    public async Task Update_persists_the_change_and_masks_the_mobile_number_in_the_response()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var saveResponse = await client.PutAsJsonAsync("/api/v1/customers/CUST-100003/kyc",
            new Dictionary<string, object?> { ["lastName"] = "Updated By Test", ["mobileNo"] = "0511112222" });

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        var saveBody = await saveResponse.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("Updated By Test", saveBody!.Data!["lastName"]?.ToString());

        var afterGet = await (await client.GetAsync("/api/v1/customers/CUST-100003/kyc"))
            .Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("Updated By Test", afterGet!.Data!["lastName"]?.ToString());
        Assert.Equal("0511112222", afterGet.Data["mobileNo"]?.ToString());

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
        var mobileAudit = await dbContext.AuditLogs
            .Where(a => a.CustomerId == "CUST-100003" && a.Field == "mobileNo")
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();
        Assert.NotNull(mobileAudit);
        Assert.EndsWith("2222", mobileAudit!.NewValue);
        Assert.Contains('*', mobileAudit.NewValue!);
    }

    [Fact]
    public async Task Update_ignores_fields_outside_the_kyc_catalog()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var response = await client.PutAsJsonAsync("/api/v1/customers/CUST-100001/kyc",
            new Dictionary<string, object?> { ["thisFieldDoesNotExist"] = "should be silently dropped" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.False(body!.Data!.ContainsKey("thisFieldDoesNotExist"));
    }
}
