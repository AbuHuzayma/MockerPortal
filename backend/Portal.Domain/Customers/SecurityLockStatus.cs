namespace Portal.Domain.Customers;

/// <summary>
/// Master spec §17 — a small, concretely-named field set (unlike KYC/IVR),
/// so a typed record is the better fit here rather than a key→value bag.
/// </summary>
public sealed record SecurityLockStatus
{
    public required string CustId { get; init; }
    public required string CustNumber { get; init; }
    public int FailedLogonCount { get; init; }
    public int FailedOtpCount { get; init; }
    public string? CurrentOtpStatus { get; init; }
    public string? FreezeStatusId { get; init; }
}
