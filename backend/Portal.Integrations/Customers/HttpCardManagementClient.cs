using System.Net.Http.Json;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Integrations.Customers;

/// <summary>
/// ASSUMED CONTRACT — replace when the real Card Management System API spec
/// is supplied (docs/08-integration-architecture.md §4). Registered via
/// HttpClientFactory in Portal.Infrastructure.DependencyInjection with a
/// base address from configuration and a request timeout; add Polly
/// retry/circuit-breaker policies once a real endpoint exists to tune against
/// (docs/08 §5 gives the default policy shape).
/// </summary>
public sealed class HttpCardManagementClient(HttpClient httpClient) : ICardManagementClient
{
    public async Task<CardStatus> GetStatusAsync(string customerId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/cards/{customerId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardStatus>(ct))!;
    }

    public async Task<CardStatus> CreateCardAsync(string customerId, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync("/cards", new { customerId }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardStatus>(ct))!;
    }

    public async Task<CardStatus> ActivateCardAsync(string customerId, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync($"/cards/{customerId}/activate", new { }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardStatus>(ct))!;
    }
}
