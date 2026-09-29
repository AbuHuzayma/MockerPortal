using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface IIvrService
{
    Task<Result<IvrRecord>> GetIvrAsync(string customerId, CancellationToken ct);

    Task<Result<IvrUpdateOutcome>> UpdateIvrAsync(string customerId, IReadOnlyDictionary<string, object?> requestedFields, CancellationToken ct);
}

public sealed record IvrUpdateOutcome(IvrRecord Before, IvrRecord After);
