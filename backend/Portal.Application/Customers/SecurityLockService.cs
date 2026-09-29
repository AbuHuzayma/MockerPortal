using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public sealed class SecurityLockService(ISecurityLockProvider provider) : ISecurityLockService
{
    public async Task<Result<SecurityLockStatus>> GetStatusAsync(string customerId, CancellationToken ct)
    {
        var status = await provider.GetStatusAsync(customerId, ct);
        return status is null
            ? Result<SecurityLockStatus>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.")
            : Result<SecurityLockStatus>.Success(status);
    }

    public async Task<Result<SecurityLockRemoveOutcome>> RemoveLockAsync(string customerId, CancellationToken ct)
    {
        var before = await provider.GetStatusAsync(customerId, ct);
        if (before is null)
        {
            return Result<SecurityLockRemoveOutcome>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.");
        }

        var after = await provider.RemoveLockAsync(customerId, ct);
        return Result<SecurityLockRemoveOutcome>.Success(new SecurityLockRemoveOutcome(before, after));
    }
}
