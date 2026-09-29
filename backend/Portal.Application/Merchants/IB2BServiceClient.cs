using Portal.Domain.Merchants;

namespace Portal.Application.Merchants;

/// <summary>
/// ASSUMED CONTRACT — the real B2B Subscription Matrix API shape has not
/// been supplied (docs/08 §4). Explicit command per master spec §20:
/// AddMerchantToB2B.
/// </summary>
public interface IB2BServiceClient
{
    Task<B2BStatus> GetStatusAsync(string merchantId, CancellationToken ct);

    Task<B2BStatus> AddMerchantToB2BAsync(string merchantId, CancellationToken ct);
}
