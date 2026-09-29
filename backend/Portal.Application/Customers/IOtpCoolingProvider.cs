using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface IOtpCoolingProvider
{
    Task<OtpCoolingRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct);

    Task<OtpCoolingRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct);
}
