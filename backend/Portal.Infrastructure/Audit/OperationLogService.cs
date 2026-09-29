using Portal.Application.Audit;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Audit;

public sealed class OperationLogService(PortalDbContext dbContext) : IOperationLogService
{
    public async Task WriteAsync(string correlationId, string commandName, long durationMs, string result, CancellationToken ct)
    {
        dbContext.OperationLogs.Add(new OperationLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            CommandName = commandName,
            DurationMs = durationMs,
            Result = result,
            Timestamp = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(ct);
    }
}
