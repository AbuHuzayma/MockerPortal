using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface IBiometricProvider
{
    Task<BiometricRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct);

    Task<BiometricRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct);
}
