using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public interface ISecurityLockService
{
    Task<Result<SecurityLockStatus>> GetStatusAsync(string customerId, CancellationToken ct);

    Task<Result<SecurityLockRemoveOutcome>> RemoveLockAsync(string customerId, CancellationToken ct);
}

public sealed record SecurityLockRemoveOutcome(SecurityLockStatus Before, SecurityLockStatus After);
