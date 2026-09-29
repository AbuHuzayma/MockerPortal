namespace Portal.Infrastructure.Audit;

/// <summary>EF entity for the append-only AuditLogs table — see docs/05-database-design.md §2.</summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public required string Username { get; set; }
    public required string Environment { get; set; }
    public string? CustomerId { get; set; }
    public string? MerchantId { get; set; }
    public required string Screen { get; set; }
    public required string Operation { get; set; }
    public required string Entity { get; set; }
    public string? Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public required string Result { get; set; }
    public string? ErrorMessage { get; set; }
    public required string CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
