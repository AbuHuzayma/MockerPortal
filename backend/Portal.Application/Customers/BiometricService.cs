using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public sealed class BiometricService(IBiometricProvider provider) : IBiometricService
{
    public async Task<Result<BiometricRecord>> GetBiometricAsync(string customerId, CancellationToken ct)
    {
        var record = await provider.GetByCustomerIdAsync(customerId, ct);
        return record is null
            ? Result<BiometricRecord>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.")
            : Result<BiometricRecord>.Success(record);
    }

    public async Task<Result<BiometricUpdateOutcome>> UpdateBiometricAsync(
        string customerId,
        IReadOnlyDictionary<string, object?> requestedFields,
        CancellationToken ct)
    {
        var before = await provider.GetByCustomerIdAsync(customerId, ct);
        if (before is null)
        {
            return Result<BiometricUpdateOutcome>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.");
        }

        var whitelisted = requestedFields
            .Where(kvp => BiometricFieldCatalog.ValidKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var after = await provider.UpdateAsync(customerId, whitelisted, ct);

        return Result<BiometricUpdateOutcome>.Success(new BiometricUpdateOutcome(before, after));
    }
}
