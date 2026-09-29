using System.Collections.Concurrent;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>In-memory security lock store — CUST-100003 is seeded "locked" to make the reset action demonstrable.</summary>
public sealed class MockSecurityLockProvider : ISecurityLockProvider
{
    private readonly ConcurrentDictionary<string, SecurityLockStatus> _store = new(new Dictionary<string, SecurityLockStatus>
    {
        ["CUST-100001"] = new() { CustId = "CUST-100001", CustNumber = "100001", FailedLogonCount = 0, FailedOtpCount = 0, CurrentOtpStatus = "Clear", FreezeStatusId = null },
        ["CUST-100002"] = new() { CustId = "CUST-100002", CustNumber = "100002", FailedLogonCount = 0, FailedOtpCount = 0, CurrentOtpStatus = "Clear", FreezeStatusId = null },
        ["CUST-100003"] = new() { CustId = "CUST-100003", CustNumber = "100003", FailedLogonCount = 5, FailedOtpCount = 3, CurrentOtpStatus = "Locked", FreezeStatusId = "1" },
    });

    public Task<SecurityLockStatus?> GetStatusAsync(string customerId, CancellationToken ct)
    {
        var status = _store.Values.FirstOrDefault(s => s.CustId == customerId || s.CustNumber == customerId);
        return Task.FromResult(status);
    }

    public Task<SecurityLockStatus> RemoveLockAsync(string customerId, CancellationToken ct)
    {
        var existing = _store.Values.FirstOrDefault(s => s.CustId == customerId || s.CustNumber == customerId)
            ?? throw new InvalidOperationException($"Unknown customer '{customerId}'.");

        var reset = existing with { FailedLogonCount = 0, FailedOtpCount = 0, CurrentOtpStatus = "Clear", FreezeStatusId = null };
        _store[existing.CustId] = reset;
        return Task.FromResult(reset);
    }
}
