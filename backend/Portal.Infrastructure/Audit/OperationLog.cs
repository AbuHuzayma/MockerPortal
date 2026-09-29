namespace Portal.Infrastructure.Audit;

/// <summary>EF entity for the append-only OperationLogs table — a technical
/// execution log distinct from the business-facing AuditLogs; see
/// docs/10-audit.md §6 and docs/05-database-design.md §2.</summary>
public sealed class OperationLog
{
    public Guid Id { get; set; }
    public required string CorrelationId { get; set; }
    public required string CommandName { get; set; }
    public long DurationMs { get; set; }
    public required string Result { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
