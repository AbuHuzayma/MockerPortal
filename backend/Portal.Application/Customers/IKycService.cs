using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface IKycService
{
    Task<Result<KycRecord>> GetKycAsync(string customerId, CancellationToken ct);

    /// <param name="requestedFields">Raw input — the service whitelists against KycFieldCatalog before persisting.</param>
    Task<Result<KycUpdateOutcome>> UpdateKycAsync(string customerId, IReadOnlyDictionary<string, object?> requestedFields, CancellationToken ct);
}

/// <summary>Before/after so the caller (controller) can write one AuditEntry per changed field.</summary>
public sealed record KycUpdateOutcome(KycRecord Before, KycRecord After);
