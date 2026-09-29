using Portal.Domain.Customers;

namespace Portal.Application.Customers;

/// <summary>Master spec §17 — fields: FAILED_LOGON_COUNT, FAILED_OTP_COUNT, CURRENT_OTP_STATUS, FREEZE_STATUS_ID.</summary>
public interface ISecurityLockProvider
{
    Task<SecurityLockStatus?> GetStatusAsync(string customerId, CancellationToken ct);

    /// <summary>Resets FailedLogonCount/FailedOtpCount to 0, clears CurrentOtpStatus/FreezeStatusId.</summary>
    Task<SecurityLockStatus> RemoveLockAsync(string customerId, CancellationToken ct);
}
