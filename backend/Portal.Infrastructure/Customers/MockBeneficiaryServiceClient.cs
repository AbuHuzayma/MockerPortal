using System.Collections.Concurrent;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>In-memory Beneficiary Service stand-in — one pending beneficiary per customer.</summary>
public sealed class MockBeneficiaryServiceClient : IBeneficiaryServiceClient
{
    private readonly ConcurrentDictionary<string, BeneficiaryStatus> _store = new();

    public Task<BeneficiaryStatus> GetStatusAsync(string customerId, string beneficiaryId, CancellationToken ct) =>
        Task.FromResult(_store.GetOrAdd(Key(customerId, beneficiaryId), _ =>
            new BeneficiaryStatus { CustomerId = customerId, BeneficiaryId = beneficiaryId, Status = "Pending" }));

    public Task<BeneficiaryStatus> ActivateBeneficiaryAsync(string customerId, string beneficiaryId, CancellationToken ct)
    {
        var activated = new BeneficiaryStatus { CustomerId = customerId, BeneficiaryId = beneficiaryId, Status = "Active" };
        _store[Key(customerId, beneficiaryId)] = activated;
        return Task.FromResult(activated);
    }

    private static string Key(string customerId, string beneficiaryId) => $"{customerId}:{beneficiaryId}";
}
