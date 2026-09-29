namespace Portal.Application.Audit;

public interface IAuditService
{
    Task WriteAsync(AuditEntry entry, CancellationToken ct);

    Task WriteAsync(IReadOnlyCollection<AuditEntry> entries, CancellationToken ct);
}
