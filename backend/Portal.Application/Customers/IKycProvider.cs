using Portal.Domain.Customers;

namespace Portal.Application.Customers;

/// <summary>
/// Kept separate from ICustomerProvider so KYC can be re-pointed independently
/// of the basic customer profile read, even though both currently read/write
/// T_PRT_CUSTOMER — see docs/08-integration-architecture.md §2.
/// </summary>
public interface IKycProvider
{
    Task<KycRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct);

    /// <param name="fields">Already filtered to KycFieldCatalog.ValidKeys by the caller.</param>
    Task<KycRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct);
}
