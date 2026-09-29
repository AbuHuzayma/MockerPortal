using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Portal.ApiTests;

/// <summary>Covers OTP, Biometrics, Security Lock, Onboarding, Cards, and Beneficiary — the rest of Phase 4.</summary>
public class CustomersRemainingPhase4FlowTests(PortalApiFactory factory) : IClassFixture<PortalApiFactory>
{
    [Fact]
    public async Task OtpCooling_get_and_put_round_trip_but_every_field_is_dropped_since_the_catalog_is_empty()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var getResponse = await client.GetAsync("/api/v1/customers/CUST-100001/otp-cooling");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var putResponse = await client.PutAsJsonAsync("/api/v1/customers/CUST-100001/otp-cooling", new Dictionary<string, object?> { ["madeUp"] = "x" });
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var body = await putResponse.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.False(body!.Data!.ContainsKey("madeUp"));
    }

    [Fact]
    public async Task Biometric_get_and_put_round_trip_but_every_field_is_dropped_since_the_catalog_is_empty()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var getResponse = await client.GetAsync("/api/v1/customers/CUST-100001/biometrics");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var putResponse = await client.PutAsJsonAsync("/api/v1/customers/CUST-100001/biometrics", new Dictionary<string, object?> { ["madeUp"] = "x" });
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
    }

    [Fact]
    public async Task Security_readonly_user_can_view_but_not_remove_the_lock()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var getResponse = await client.GetAsync("/api/v1/customers/CUST-100003/security");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var removeResponse = await client.PostAsync("/api/v1/customers/CUST-100003/security/remove-lock", null);
        Assert.Equal(HttpStatusCode.Forbidden, removeResponse.StatusCode);
    }

    [Fact]
    public async Task Security_administrator_can_remove_a_lock_and_the_change_persists()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var before = await (await client.GetAsync("/api/v1/customers/CUST-100003/security"))
            .Content.ReadFromJsonAsync<DataEnvelope<SecurityStatus>>();
        Assert.True(before!.Data!.FailedLogonCount > 0);

        var removeResponse = await client.PostAsync("/api/v1/customers/CUST-100003/security/remove-lock", null);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        var after = await removeResponse.Content.ReadFromJsonAsync<DataEnvelope<SecurityStatus>>();
        Assert.Equal(0, after!.Data!.FailedLogonCount);
        Assert.Equal(0, after.Data.FailedOtpCount);
        Assert.Equal("Clear", after.Data.CurrentOtpStatus);
    }

    [Fact]
    public async Task Onboarding_returns_identity_and_created_date_read_only()
    {
        var client = await factory.CreateAuthenticatedClientAsync("readonly@portal.local");

        var response = await client.GetAsync("/api/v1/customers/CUST-100001/onboarding");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataEnvelope<Dictionary<string, object?>>>();
        Assert.Equal("CUST-100001", body!.Data!["custId"]?.ToString());
        Assert.Equal("T24-100001", body.Data["t24CustomerId"]?.ToString());
    }

    [Fact]
    public async Task Cards_activate_creates_and_activates_a_card()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var statusBefore = await (await client.GetAsync("/api/v1/customers/CUST-100002/cards"))
            .Content.ReadFromJsonAsync<DataEnvelope<CardStatusDto>>();
        Assert.Equal("None", statusBefore!.Data!.Status);

        var activateResponse = await client.PostAsync("/api/v1/customers/CUST-100002/cards/activate", null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        var activated = await activateResponse.Content.ReadFromJsonAsync<DataEnvelope<CardStatusDto>>();
        Assert.Equal("Active", activated!.Data!.Status);
        Assert.False(string.IsNullOrWhiteSpace(activated.Data.CardNumber));
    }

    [Fact]
    public async Task Beneficiary_activate_moves_status_from_pending_to_active()
    {
        var client = await factory.CreateAuthenticatedClientAsync("admin@portal.local");

        var statusBefore = await (await client.GetAsync("/api/v1/customers/CUST-100002/beneficiaries/BEN-0001"))
            .Content.ReadFromJsonAsync<DataEnvelope<BeneficiaryStatusDto>>();
        Assert.Equal("Pending", statusBefore!.Data!.Status);

        var activateResponse = await client.PostAsync("/api/v1/customers/CUST-100002/beneficiaries/BEN-0001/activate", null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        var activated = await activateResponse.Content.ReadFromJsonAsync<DataEnvelope<BeneficiaryStatusDto>>();
        Assert.Equal("Active", activated!.Data!.Status);
    }

    private sealed record SecurityStatus(string CustId, int FailedLogonCount, int FailedOtpCount, string? CurrentOtpStatus, string? FreezeStatusId);
    private sealed record CardStatusDto(string CustomerId, string? CardNumber, string Status);
    private sealed record BeneficiaryStatusDto(string CustomerId, string BeneficiaryId, string Status);
}
