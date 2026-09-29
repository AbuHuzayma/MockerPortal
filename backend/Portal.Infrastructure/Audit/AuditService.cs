using Portal.Application.Audit;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Audit;

public sealed class AuditService(PortalDbContext dbContext) : IAuditService
{
    public async Task WriteAsync(AuditEntry entry, CancellationToken ct)
    {
        dbContext.AuditLogs.Add(ToEntity(entry));
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task WriteAsync(IReadOnlyCollection<AuditEntry> entries, CancellationToken ct)
    {
        if (entries.Count == 0)
        {
            return;
        }

        dbContext.AuditLogs.AddRange(entries.Select(ToEntity));
        await dbContext.SaveChangesAsync(ct);
    }

    private static AuditLog ToEntity(AuditEntry entry) => new()
    {
        Id = Guid.NewGuid(),
        UserId = entry.UserId,
        Username = entry.Username,
        Environment = entry.Environment,
        CustomerId = entry.CustomerId,
        MerchantId = entry.MerchantId,
        Screen = entry.Screen,
        Operation = entry.Operation,
        Entity = entry.Entity,
        Field = entry.Field,
        OldValue = entry.OldValue,
        NewValue = entry.NewValue,
        Result = entry.Result,
        ErrorMessage = entry.ErrorMessage,
        CorrelationId = entry.CorrelationId,
        Timestamp = DateTime.UtcNow,
    };
}
