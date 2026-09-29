using System.Collections.Concurrent;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>
/// In-memory, process-lifetime KYC store for local/DEV use, seeded for the same
/// three customers as MockCustomerProvider. Selected via Providers:Kyc:Mode
/// = "Mock" (the default everywhere except QA/PREPROD).
/// </summary>
public sealed class MockKycProvider : IKycProvider
{
    private static readonly (string CustId, string CustNumber, Dictionary<string, object?> Fields)[] Seed =
    [
        ("CUST-100001", "100001", new Dictionary<string, object?>
        {
            ["mobileNo"] = "0500000001",
            ["firstName"] = "Sara",
            ["lastName"] = "Al-Otaibi",
            ["arabicFirstName"] = "سارة",
            ["arabicLastName"] = "العتيبي",
            ["email"] = "sara.test@example.com",
            ["nationality"] = "SA",
            ["lifeStatus"] = "Active",
            ["blacklistStatus"] = "Clear",
            ["t24CustomerId"] = "T24-100001",
            ["kycLevelId"] = "2",
            ["pepByScreening"] = false,
            ["pepByCustomer"] = false,
            ["pepByProfession"] = false,
            ["pepByRelationship"] = false,
        }),
        ("CUST-100002", "100002", new Dictionary<string, object?>
        {
            ["mobileNo"] = "0500000002",
            ["firstName"] = "Omar",
            ["lastName"] = "Al-Harbi",
            ["arabicFirstName"] = "عمر",
            ["arabicLastName"] = "الحربي",
            ["email"] = "omar.test@example.com",
            ["nationality"] = "SA",
            ["lifeStatus"] = "Active",
            ["blacklistStatus"] = "Clear",
            ["t24CustomerId"] = "T24-100002",
            ["kycLevelId"] = "1",
            ["pepByScreening"] = false,
            ["pepByCustomer"] = false,
            ["pepByProfession"] = false,
            ["pepByRelationship"] = false,
        }),
        ("CUST-100003", "100003", new Dictionary<string, object?>
        {
            ["mobileNo"] = "0500000003",
            ["firstName"] = "Layla",
            ["lastName"] = "Al-Zahrani",
            ["arabicFirstName"] = "ليلى",
            ["arabicLastName"] = "الزهراني",
            ["nationality"] = "SA",
            ["lifeStatus"] = "Suspended",
            ["blacklistStatus"] = "Clear",
            ["kycLevelId"] = "1",
            ["pepByScreening"] = true,
            ["pepByCustomer"] = false,
            ["pepByProfession"] = false,
            ["pepByRelationship"] = false,
        }),
    ];

    private readonly ConcurrentDictionary<string, (string CustNumber, Dictionary<string, object?> Fields)> _store =
        new(Seed.ToDictionary(s => s.CustId, s => (s.CustNumber, s.Fields)));

    public Task<KycRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct)
    {
        var entry = _store.FirstOrDefault(kvp => kvp.Key == customerId || kvp.Value.CustNumber == customerId);
        if (entry.Key is null)
        {
            return Task.FromResult<KycRecord?>(null);
        }

        return Task.FromResult<KycRecord?>(new KycRecord
        {
            CustId = entry.Key,
            CustNumber = entry.Value.CustNumber,
            Fields = new Dictionary<string, object?>(entry.Value.Fields),
        });
    }

    public Task<KycRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct)
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

        return Task.FromResult(new KycRecord { CustId = key, CustNumber = custNumber, Fields = new Dictionary<string, object?>(merged) });
    }
}
