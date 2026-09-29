namespace Portal.Application.Audit;

public sealed record AuditLogDto(
    Guid Id,
    string Username,
    string Environment,
    string? CustomerId,
    string? MerchantId,
    string Screen,
    string Operation,
    string Entity,
    string? Field,
    string? OldValue,
    string? NewValue,
    string Result,
    string? ErrorMessage,
    string CorrelationId,
    DateTime Timestamp);

public sealed record AuditLogQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Username,
    string? CustomerId,
    string? MerchantId,
    string? Screen,
    string? Entity,
    string? Result,
    int Page = 1,
    int PageSize = 50);

public sealed record AuditLogPage(IReadOnlyList<AuditLogDto> Items, int TotalCount, int Page, int PageSize);

public interface IAuditQueryService
{
    Task<AuditLogPage> QueryAsync(AuditLogQuery query, CancellationToken ct);
}
