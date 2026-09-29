using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface ICreationService
{
    Task<Result<CreationRecord>> GetCreationAsync(string customerId, CancellationToken ct);

    Task<Result<CreationUpdateOutcome>> UpdateCreationAsync(string customerId, IReadOnlyDictionary<string, object?> requestedFields, CancellationToken ct);
}

public sealed record CreationUpdateOutcome(CreationRecord Before, CreationRecord After);
