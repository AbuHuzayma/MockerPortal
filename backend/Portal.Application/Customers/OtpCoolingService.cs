using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public sealed class OtpCoolingService(IOtpCoolingProvider provider) : IOtpCoolingService
{
    public async Task<Result<OtpCoolingRecord>> GetOtpCoolingAsync(string customerId, CancellationToken ct)
    {
        var record = await provider.GetByCustomerIdAsync(customerId, ct);
        return record is null
            ? Result<OtpCoolingRecord>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.")
            : Result<OtpCoolingRecord>.Success(record);
    }

    public async Task<Result<OtpCoolingUpdateOutcome>> UpdateOtpCoolingAsync(
        string customerId,
        IReadOnlyDictionary<string, object?> requestedFields,
        CancellationToken ct)
    {
        var before = await provider.GetByCustomerIdAsync(customerId, ct);
        if (before is null)
        {
            return Result<OtpCoolingUpdateOutcome>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.");
        }

        var whitelisted = requestedFields
            .Where(kvp => OtpCoolingFieldCatalog.ValidKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var after = await provider.UpdateAsync(customerId, whitelisted, ct);

        return Result<OtpCoolingUpdateOutcome>.Success(new OtpCoolingUpdateOutcome(before, after));
    }
}
