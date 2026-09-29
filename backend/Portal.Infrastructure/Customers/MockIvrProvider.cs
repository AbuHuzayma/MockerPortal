using System.Collections.Concurrent;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>
/// In-memory IVR store for local/DEV use, seeded (with empty field sets — see
/// IvrFieldCatalog) for the same three customers as MockCustomerProvider.
/// This is the only IIvrProvider implementation that exists: there is no
/// SqlIvrProvider, since the IVR database schema is unknown — see
/// docs/05-database-design.md §3 and DependencyInjection.AddInfrastructure.
/// </summary>
public sealed class MockIvrProvider : IIvrProvider
{
    private static readonly (string CustId, string CustNumber)[] Seed =
    [
        ("CUST-100001", "100001"),
        ("CUST-100002", "100002"),
        ("CUST-100003", "100003"),
    ];

    private readonly ConcurrentDictionary<string, (string CustNumber, Dictionary<string, object?> Fields)> _store =
        new(Seed.ToDictionary(s => s.CustId, s => (s.CustNumber, new Dictionary<string, object?>())));

    public Task<IvrRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct)
    {
        var entry = _store.FirstOrDefault(kvp => kvp.Key == customerId || kvp.Value.CustNumber == customerId);
        if (entry.Key is null)
        {
            return Task.FromResult<IvrRecord?>(null);
        }

        return Task.FromResult<IvrRecord?>(new IvrRecord
        {
            CustId = entry.Key,
            CustNumber = entry.Value.CustNumber,
            Fields = new Dictionary<string, object?>(entry.Value.Fields),
        });
    }

    public Task<IvrRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct)
    {
        var key = _store.Keys.FirstOrDefault(k => k == customerId || _store[k].CustNumber == customerId)
            ?? throw new InvalidOperationException($"Unknown customer '{customerId}'.");

        var (custNumber, existing) = _store[key];
        var merged = new Dictionary<string, object?>(existing);
        foreach (var (fieldKey, value) in fields)
        {
            merged[fieldKey] = value;
        }
        _store[key] = (custNumber, merged);

        return Task.FromResult(new IvrRecord { CustId = key, CustNumber = custNumber, Fields = new Dictionary<string, object?>(merged) });
    }
}
