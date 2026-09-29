namespace Portal.Application.Audit;

/// <summary>Technical execution log distinct from the business-facing IAuditService — see docs/10-audit.md §6.</summary>
public interface IOperationLogService
{
    Task WriteAsync(string correlationId, string commandName, long durationMs, string result, CancellationToken ct);
}
