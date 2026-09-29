using System.Net.Http.Json;
using Portal.Application.Merchants;
using Portal.Domain.Merchants;

namespace Portal.Integrations.Merchants;

/// <summary>ASSUMED CONTRACT — replace when the real B2B Subscription Matrix API spec is supplied (docs/08 §4).</summary>
public sealed class HttpB2BServiceClient(HttpClient httpClient) : IB2BServiceClient
{
    public async Task<B2BStatus> GetStatusAsync(string merchantId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/b2b/{merchantId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<B2BStatus>(ct))!;
    }

    public async Task<B2BStatus> AddMerchantToB2BAsync(string merchantId, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync($"/b2b/{merchantId}", new { }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<B2BStatus>(ct))!;
    }
}
