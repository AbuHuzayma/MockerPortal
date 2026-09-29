using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface ICreationProvider
{
    Task<CreationRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct);

    /// <param name="fields">Already filtered to CreationFieldCatalog.ValidKeys by the caller.</param>
    Task<CreationRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct);
}
