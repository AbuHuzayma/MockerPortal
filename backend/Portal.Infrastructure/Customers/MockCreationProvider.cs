using System.Collections.Concurrent;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>
/// In-memory Creation-details store for local/DEV use, seeded for the same
/// three customers as MockCustomerProvider. Selected via
/// Providers:Creation:Mode = "Mock" (the default everywhere except QA/PREPROD).
/// </summary>
public sealed class MockCreationProvider : ICreationProvider
{
    private static readonly (string CustId, string CustNumber, Dictionary<string, object?> Fields)[] Seed =
    [
        ("CUST-100001", "100001", new Dictionary<string, object?>
        {
            ["createdDate"] = "2022-03-10",
            ["dateOfBirth"] = "1994-07-22",
            ["kycDate"] = "2022-03-12",
        }),
        ("CUST-100002", "100002", new Dictionary<string, object?>
        {
            ["createdDate"] = "2023-01-05",
            ["dateOfBirth"] = "1988-11-02",
            ["kycDate"] = "2023-01-06",
        }),
        ("CUST-100003", "100003", new Dictionary<string, object?>
        {
            ["createdDate"] = "2021-09-18",
            ["dateOfBirth"] = "2001-02-14",
            ["kycDate"] = null,
        }),
    ];

    private readonly ConcurrentDictionary<string, (string CustNumber, Dictionary<string, object?> Fields)> _store =
        new(Seed.ToDictionary(s => s.CustId, s => (s.CustNumber, s.Fields)));

    public Task<CreationRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct)
    {
        var entry = _store.FirstOrDefault(kvp => kvp.Key == customerId || kvp.Value.CustNumber == customerId);
        if (entry.Key is null)
        {
            return Task.FromResult<CreationRecord?>(null);
        }

        return Task.FromResult<CreationRecord?>(new CreationRecord
        {
            CustId = entry.Key,
            CustNumber = entry.Value.CustNumber,
            Fields = new Dictionary<string, object?>(entry.Value.Fields),
        });
    }

    public Task<CreationRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct)
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

        return Task.FromResult(new CreationRecord { CustId = key, CustNumber = custNumber, Fields = new Dictionary<string, object?>(merged) });
    }
}
