using Portal.Domain.Merchants;

namespace Portal.Application.Merchants;

/// <summary>
/// Master spec §16 — Merchant DB (MSSQL). No search key was specified, so
/// name (partial match) is used, mirroring Customer's "search by the one
/// human-findable field" approach.
/// </summary>
public interface IMerchantProvider
{
    Task<IReadOnlyList<Merchant>> SearchByNameAsync(string name, CancellationToken ct);

    Task<Merchant?> FindByMerchantIdAsync(string merchantId, CancellationToken ct);

    Task<Merchant> UpdateAsync(string merchantId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct);
}
