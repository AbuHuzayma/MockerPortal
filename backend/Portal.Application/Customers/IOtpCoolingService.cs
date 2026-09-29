using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface IOtpCoolingService
{
    Task<Result<OtpCoolingRecord>> GetOtpCoolingAsync(string customerId, CancellationToken ct);

    Task<Result<OtpCoolingUpdateOutcome>> UpdateOtpCoolingAsync(string customerId, IReadOnlyDictionary<string, object?> requestedFields, CancellationToken ct);
}

public sealed record OtpCoolingUpdateOutcome(OtpCoolingRecord Before, OtpCoolingRecord After);
