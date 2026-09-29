using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public sealed class CreationService(ICreationProvider provider) : ICreationService
{
    public async Task<Result<CreationRecord>> GetCreationAsync(string customerId, CancellationToken ct)
    {
        var record = await provider.GetByCustomerIdAsync(customerId, ct);
        return record is null
            ? Result<CreationRecord>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.")
            : Result<CreationRecord>.Success(record);
    }

    public async Task<Result<CreationUpdateOutcome>> UpdateCreationAsync(
        string customerId,
        IReadOnlyDictionary<string, object?> requestedFields,
        CancellationToken ct)
    {
        var before = await provider.GetByCustomerIdAsync(customerId, ct);
        if (before is null)
        {
            return Result<CreationUpdateOutcome>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.");
        }

        var whitelisted = requestedFields
            .Where(kvp => CreationFieldCatalog.ValidKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var after = await provider.UpdateAsync(customerId, whitelisted, ct);

        return Result<CreationUpdateOutcome>.Success(new CreationUpdateOutcome(before, after));
    }
}
