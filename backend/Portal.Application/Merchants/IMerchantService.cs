using Portal.Application.Common;
using Portal.Domain.Merchants;

namespace Portal.Application.Merchants;

public static class MerchantErrorCodes
{
    public const string MerchantNotFound = "MERCHANT_NOT_FOUND";
}

public interface IMerchantService
{
    Task<Result<IReadOnlyList<Merchant>>> SearchByNameAsync(string name, CancellationToken ct);

    Task<Result<Merchant>> GetProfileAsync(string merchantId, CancellationToken ct);

    Task<Result<MerchantUpdateOutcome>> UpdateAsync(string merchantId, IReadOnlyDictionary<string, object?> requestedFields, CancellationToken ct);
}

public sealed record MerchantUpdateOutcome(Merchant Before, Merchant After);
