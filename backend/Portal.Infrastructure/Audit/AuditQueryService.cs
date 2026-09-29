using Microsoft.EntityFrameworkCore;
using Portal.Application.Audit;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Audit;

public sealed class AuditQueryService(PortalDbContext dbContext) : IAuditQueryService
{
    public async Task<AuditLogPage> QueryAsync(AuditLogQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var filtered = dbContext.AuditLogs.AsQueryable();

        if (query.FromUtc is { } from)
        {
            filtered = filtered.Where(a => a.Timestamp >= from);
        }
        if (query.ToUtc is { } to)
        {
            filtered = filtered.Where(a => a.Timestamp <= to);
        }
        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            filtered = filtered.Where(a => a.Username.Contains(query.Username));
        }
        if (!string.IsNullOrWhiteSpace(query.CustomerId))
        {
            filtered = filtered.Where(a => a.CustomerId == query.CustomerId);
        }
        if (!string.IsNullOrWhiteSpace(query.MerchantId))
        {
            filtered = filtered.Where(a => a.MerchantId == query.MerchantId);
        }
        if (!string.IsNullOrWhiteSpace(query.Screen))
        {
            filtered = filtered.Where(a => a.Screen == query.Screen);
        }
        if (!string.IsNullOrWhiteSpace(query.Entity))
        {
            filtered = filtered.Where(a => a.Entity == query.Entity);
        }
        if (!string.IsNullOrWhiteSpace(query.Result))
        {
            filtered = filtered.Where(a => a.Result == query.Result);
        }

        var totalCount = await filtered.CountAsync(ct);

        var items = await filtered
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.Username, a.Environment, a.CustomerId, a.MerchantId, a.Screen, a.Operation, a.Entity,
                a.Field, a.OldValue, a.NewValue, a.Result, a.ErrorMessage, a.CorrelationId, a.Timestamp))
            .ToListAsync(ct);

        return new AuditLogPage(items, totalCount, page, pageSize);
    }
}
