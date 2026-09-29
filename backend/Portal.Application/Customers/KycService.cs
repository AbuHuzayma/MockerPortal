using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public sealed class KycService(IKycProvider provider) : IKycService
{
    public async Task<Result<KycRecord>> GetKycAsync(string customerId, CancellationToken ct)
    {
        var record = await provider.GetByCustomerIdAsync(customerId, ct);
        return record is null
            ? Result<KycRecord>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.")
            : Result<KycRecord>.Success(WithComputedPep(record));
    }

    public async Task<Result<KycUpdateOutcome>> UpdateKycAsync(
        string customerId,
        IReadOnlyDictionary<string, object?> requestedFields,
        CancellationToken ct)
    {
        var before = await provider.GetByCustomerIdAsync(customerId, ct);
        if (before is null)
        {
            return Result<KycUpdateOutcome>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.");
        }

        // The whitelist: a field can only be written if it's in KycFieldCatalog,
        // regardless of what the caller sent — see docs/07 §3.
        var whitelisted = requestedFields
            .Where(kvp => KycFieldCatalog.ValidKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var after = await provider.UpdateAsync(customerId, whitelisted, ct);

        return Result<KycUpdateOutcome>.Success(new KycUpdateOutcome(WithComputedPep(before), WithComputedPep(after)));
    }

    private static KycRecord WithComputedPep(KycRecord record)
    {
        var fields = new Dictionary<string, object?>(record.Fields) { ["pep"] = record.ComputePep() };
        return new KycRecord { CustId = record.CustId, CustNumber = record.CustNumber, Fields = fields };
    }
}
