using Microsoft.EntityFrameworkCore;
using Portal.Application.Audit;
using Portal.Infrastructure.Audit;
using Portal.Infrastructure.Persistence;
using Xunit;

namespace Portal.IntegrationTests.Audit;

/// <summary>
/// Smoke test for the audit plumbing (table + service) added in Phase 2 — see
/// docs/15-development-roadmap.md. Nothing in the product writes real audit
/// entries yet (no mutations exist before Phase 4); this proves the write path
/// works end-to-end against the actual EF mapping.
/// </summary>
public class AuditServiceTests
{
    private static PortalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase($"AuditServiceTests-{Guid.NewGuid()}")
            .Options;
        return new PortalDbContext(options);
    }

    [Fact]
    public async Task WriteAsync_persists_a_single_entry()
    {
        await using var dbContext = CreateContext();
        var service = new AuditService(dbContext);

        var entry = new AuditEntry
        {
            Username = "smoke-test@portal.local",
            Environment = "DEV",
            Screen = "SMOKE_TEST",
            Operation = "SMOKE_TEST",
            Entity = "SmokeTest",
            Result = AuditResults.Success,
            CorrelationId = Guid.NewGuid().ToString(),
        };

        await service.WriteAsync(entry, CancellationToken.None);

        var stored = await dbContext.AuditLogs.SingleAsync();
        Assert.Equal(entry.Username, stored.Username);
        Assert.Equal(entry.CorrelationId, stored.CorrelationId);
        Assert.Equal(AuditResults.Success, stored.Result);
    }

    [Fact]
    public async Task WriteAsync_batch_persists_one_row_per_changed_field_sharing_a_correlation_id()
    {
        await using var dbContext = CreateContext();
        var service = new AuditService(dbContext);
        var correlationId = Guid.NewGuid().ToString();

        AuditEntry ForField(string field, string oldValue, string newValue) => new()
        {
            Username = "smoke-test@portal.local",
            Environment = "DEV",
            CustomerId = "CUST-1",
            Screen = "SMOKE_TEST",
            Operation = "SMOKE_TEST_UPDATE",
            Entity = "Customer",
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
            Result = AuditResults.Success,
            CorrelationId = correlationId,
        };

        await service.WriteAsync([ForField("EMAIL", "old@x.com", "new@x.com"), ForField("FIRST_NAME", "A", "B")], CancellationToken.None);

        var stored = await dbContext.AuditLogs.Where(a => a.CorrelationId == correlationId).ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.All(stored, a => Assert.Equal(correlationId, a.CorrelationId));
    }
}
