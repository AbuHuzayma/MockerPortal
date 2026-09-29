using Portal.Application.Common;
using Portal.Domain.Merchants;

namespace Portal.Application.Merchants;

public sealed class MerchantService(IMerchantProvider provider) : IMerchantService
{
    public async Task<Result<IReadOnlyList<Merchant>>> SearchByNameAsync(string name, CancellationToken ct)
    {
        var matches = await provider.SearchByNameAsync(name, ct);
        return matches.Count == 0
            ? Result<IReadOnlyList<Merchant>>.Failure(MerchantErrorCodes.MerchantNotFound, "No merchant was found for that name.")
            : Result<IReadOnlyList<Merchant>>.Success(matches);
    }

    public async Task<Result<Merchant>> GetProfileAsync(string merchantId, CancellationToken ct)
    {
        var merchant = await provider.FindByMerchantIdAsync(merchantId, ct);
        return merchant is null
            ? Result<Merchant>.Failure(MerchantErrorCodes.MerchantNotFound, "Merchant was not found.")
            : Result<Merchant>.Success(merchant);
    }

    public async Task<Result<MerchantUpdateOutcome>> UpdateAsync(
        string merchantId,
        IReadOnlyDictionary<string, object?> requestedFields,
        CancellationToken ct)
    {
        var before = await provider.FindByMerchantIdAsync(merchantId, ct);
        if (before is null)
        {
            return Result<MerchantUpdateOutcome>.Failure(MerchantErrorCodes.MerchantNotFound, "Merchant was not found.");
        }

        var whitelisted = requestedFields
            .Where(kvp => MerchantFieldCatalog.ValidKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var after = await provider.UpdateAsync(merchantId, whitelisted, ct);

        return Result<MerchantUpdateOutcome>.Success(new MerchantUpdateOutcome(before, after));
    }
}
