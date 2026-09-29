namespace Portal.Application.Audit;

/// <summary>
/// One audit row — see docs/10-audit.md §2. For a field-level update, one
/// AuditEntry is written per changed field, sharing a CorrelationId. Masking of
/// sensitive OldValue/NewValue content (docs/10-audit.md §4) is applied by callers
/// that know which fields are sensitive — deferred until Phase 4's real KYC/
/// security-lock mutations exist to mask.
/// </summary>
public sealed record AuditEntry
{
    public Guid? UserId { get; init; }
    public required string Username { get; init; }
    public required string Environment { get; init; }
    public string? CustomerId { get; init; }
    public string? MerchantId { get; init; }
    public required string Screen { get; init; }
    public required string Operation { get; init; }
    public required string Entity { get; init; }
    public string? Field { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public required string Result { get; init; }
    public string? ErrorMessage { get; init; }
    public required string CorrelationId { get; init; }
}

public static class AuditResults
{
    public const string Success = "Success";
    public const string Failed = "Failed";
}
