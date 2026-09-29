using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface IBiometricService
{
    Task<Result<BiometricRecord>> GetBiometricAsync(string customerId, CancellationToken ct);

    Task<Result<BiometricUpdateOutcome>> UpdateBiometricAsync(string customerId, IReadOnlyDictionary<string, object?> requestedFields, CancellationToken ct);
}

public sealed record BiometricUpdateOutcome(BiometricRecord Before, BiometricRecord After);
