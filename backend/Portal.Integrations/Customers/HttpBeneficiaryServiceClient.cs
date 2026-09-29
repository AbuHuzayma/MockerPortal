using System.Net.Http.Json;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Integrations.Customers;

/// <summary>
/// ASSUMED CONTRACT — replace when the real Beneficiary Service API spec is
/// supplied (docs/08-integration-architecture.md §4).
/// </summary>
public sealed class HttpBeneficiaryServiceClient(HttpClient httpClient) : IBeneficiaryServiceClient
{
    public async Task<BeneficiaryStatus> GetStatusAsync(string customerId, string beneficiaryId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/beneficiaries/{customerId}/{beneficiaryId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BeneficiaryStatus>(ct))!;
    }

    public async Task<BeneficiaryStatus> ActivateBeneficiaryAsync(string customerId, string beneficiaryId, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync($"/beneficiaries/{customerId}/{beneficiaryId}/activate", new { }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BeneficiaryStatus>(ct))!;
    }
}
