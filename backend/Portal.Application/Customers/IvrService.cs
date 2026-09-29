using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public sealed class IvrService(IIvrProvider provider) : IIvrService
{
    public async Task<Result<IvrRecord>> GetIvrAsync(string customerId, CancellationToken ct)
    {
        var record = await provider.GetByCustomerIdAsync(customerId, ct);
        return record is null
            ? Result<IvrRecord>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.")
            : Result<IvrRecord>.Success(record);
    }

    public async Task<Result<IvrUpdateOutcome>> UpdateIvrAsync(
        string customerId,
        IReadOnlyDictionary<string, object?> requestedFields,
        CancellationToken ct)
    {
        var before = await provider.GetByCustomerIdAsync(customerId, ct);
        if (before is null)
        {
            return Result<IvrUpdateOutcome>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.");
        }

        // Whitelist against IvrFieldCatalog — currently empty, so any request
        // is filtered down to nothing until real IVR schema is supplied.
        var whitelisted = requestedFields
            .Where(kvp => IvrFieldCatalog.ValidKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var after = await provider.UpdateAsync(customerId, whitelisted, ct);

        return Result<IvrUpdateOutcome>.Success(new IvrUpdateOutcome(before, after));
    }
}
