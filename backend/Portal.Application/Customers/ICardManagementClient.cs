using Portal.Domain.Customers;

namespace Portal.Application.Customers;

/// <summary>
/// ASSUMED CONTRACT — the real Card Management System's API shape has not
/// been supplied. Method names/operations come directly from the master spec
/// §14 ("Operations should be explicit commands, for example: CreateCard,
/// ActivateCard"); request/response shapes here are this project's own
/// placeholder until a real spec exists. Real implementation (docs/08 §4)
/// goes in Portal.Integrations behind HttpClientFactory + Polly; the mock
/// implementation lives in Portal.Infrastructure since it needs no HTTP stack.
/// </summary>
public interface ICardManagementClient
{
    Task<CardStatus> GetStatusAsync(string customerId, CancellationToken ct);

    Task<CardStatus> CreateCardAsync(string customerId, CancellationToken ct);

    Task<CardStatus> ActivateCardAsync(string customerId, CancellationToken ct);
}
