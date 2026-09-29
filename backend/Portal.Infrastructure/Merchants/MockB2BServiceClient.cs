using System.Collections.Concurrent;
using Portal.Application.Merchants;
using Portal.Domain.Merchants;

namespace Portal.Infrastructure.Merchants;

/// <summary>In-memory B2B Subscription Matrix stand-in.</summary>
public sealed class MockB2BServiceClient : IB2BServiceClient
{
    private readonly ConcurrentDictionary<string, B2BStatus> _store = new();

    public Task<B2BStatus> GetStatusAsync(string merchantId, CancellationToken ct) =>
        Task.FromResult(_store.GetOrAdd(merchantId, id => new B2BStatus { MerchantId = id, Status = "NotSubscribed" }));

    public Task<B2BStatus> AddMerchantToB2BAsync(string merchantId, CancellationToken ct)
    {
        var status = new B2BStatus { MerchantId = merchantId, Status = "Subscribed" };
        _store[merchantId] = status;
        return Task.FromResult(status);
    }
}
