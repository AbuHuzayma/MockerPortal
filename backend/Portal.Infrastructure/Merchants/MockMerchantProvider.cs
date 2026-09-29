using System.Collections.Concurrent;
using Portal.Application.Merchants;
using Portal.Domain.Merchants;

namespace Portal.Infrastructure.Merchants;

/// <summary>Seeded fixture data for local/DEV use — selected via Providers:Merchant:Mode = "Mock".</summary>
public sealed class MockMerchantProvider : IMerchantProvider
{
    private readonly ConcurrentDictionary<string, Merchant> _store = new(new Dictionary<string, Merchant>
    {
        ["MERCH-200001"] = new()
        {
            MerchantId = "MERCH-200001",
            MerchantNumber = "200001",
            NameEn = "Al Noor Trading",
            NameAr = "النور للتجارة",
            BrandNameEn = "Noor Mart",
            BrandNameAr = "نور مارت",
            CrExpiryDate = "2026-05-01",
            IdExpiryDate = "2027-01-15",
        },
        ["MERCH-200002"] = new()
        {
            MerchantId = "MERCH-200002",
            MerchantNumber = "200002",
            NameEn = "Falcon Electronics",
            NameAr = "الصقر للإلكترونيات",
            BrandNameEn = "Falcon Tech",
            BrandNameAr = "فالكون تك",
            CrExpiryDate = "2025-11-20",
            IdExpiryDate = "2026-08-30",
        },
    });

    public Task<IReadOnlyList<Merchant>> SearchByNameAsync(string name, CancellationToken ct)
    {
        var matches = _store.Values
            .Where(m => (m.NameEn?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (m.NameAr?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
        return Task.FromResult<IReadOnlyList<Merchant>>(matches);
    }

    public Task<Merchant?> FindByMerchantIdAsync(string merchantId, CancellationToken ct)
    {
        var merchant = _store.Values.FirstOrDefault(m => m.MerchantId == merchantId || m.MerchantNumber == merchantId);
        return Task.FromResult(merchant);
    }

    public Task<Merchant> UpdateAsync(string merchantId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct)
    {
        var existing = _store.Values.FirstOrDefault(m => m.MerchantId == merchantId || m.MerchantNumber == merchantId)
            ?? throw new InvalidOperationException($"Unknown merchant '{merchantId}'.");

        string? Get(string key) => fields.TryGetValue(key, out var v) ? v?.ToString() : GetExisting(key);
        string? GetExisting(string key) => key switch
        {
            "nameEn" => existing.NameEn,
            "nameAr" => existing.NameAr,
            "brandNameEn" => existing.BrandNameEn,
            "brandNameAr" => existing.BrandNameAr,
            "crExpiryDate" => existing.CrExpiryDate,
            "idExpiryDate" => existing.IdExpiryDate,
            _ => null,
        };

        var updated = new Merchant
        {
            MerchantId = existing.MerchantId,
            MerchantNumber = existing.MerchantNumber,
            NameEn = Get("nameEn"),
            NameAr = Get("nameAr"),
            BrandNameEn = Get("brandNameEn"),
            BrandNameAr = Get("brandNameAr"),
            CrExpiryDate = Get("crExpiryDate"),
            IdExpiryDate = Get("idExpiryDate"),
        };
        _store[existing.MerchantId] = updated;
        return Task.FromResult(updated);
    }
}
