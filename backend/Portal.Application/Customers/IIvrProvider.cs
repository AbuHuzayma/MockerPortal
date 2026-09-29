using Portal.Domain.Customers;

namespace Portal.Application.Customers;

/// <summary>
/// Kept as its own interface (not folded into ICustomerProvider/IKycProvider)
/// per docs/08-integration-architecture.md §2, since the IVR database,
/// telephony database, and global profile engine the master spec mentions
/// (§11) are likely a genuinely different backing system than
/// T_PRT_CUSTOMER — not assumed to be the same store just because today's
/// mock implementation happens to live in the same process.
/// </summary>
public interface IIvrProvider
{
    Task<IvrRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct);

    /// <param name="fields">Already filtered to IvrFieldCatalog.ValidKeys by the caller.</param>
    Task<IvrRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct);
}
