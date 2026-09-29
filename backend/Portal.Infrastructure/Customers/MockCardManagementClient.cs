using System.Collections.Concurrent;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>In-memory Card Management System stand-in — one card per customer, progressing None → Created → Active.</summary>
public sealed class MockCardManagementClient : ICardManagementClient
{
    private readonly ConcurrentDictionary<string, CardStatus> _store = new();

    public Task<CardStatus> GetStatusAsync(string customerId, CancellationToken ct) =>
        Task.FromResult(_store.GetOrAdd(customerId, id => new CardStatus { CustomerId = id, Status = "None" }));

    public Task<CardStatus> CreateCardAsync(string customerId, CancellationToken ct)
    {
        var status = new CardStatus { CustomerId = customerId, CardNumber = GenerateCardNumber(), Status = "Created" };
        _store[customerId] = status;
        return Task.FromResult(status);
    }

    public Task<CardStatus> ActivateCardAsync(string customerId, CancellationToken ct)
    {
        var current = _store.GetOrAdd(customerId, id => new CardStatus { CustomerId = id, Status = "None" });
        var activated = current with { Status = "Active", CardNumber = current.CardNumber ?? GenerateCardNumber() };
        _store[customerId] = activated;
        return Task.FromResult(activated);
    }

    private static string GenerateCardNumber() => $"4000-{Random.Shared.Next(1000, 9999)}-{Random.Shared.Next(1000, 9999)}-{Random.Shared.Next(1000, 9999)}";
}
