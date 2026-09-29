namespace Portal.Domain.Merchants;

/// <summary>Master spec §20 — B2B Subscription Matrix, via an internal Web API.</summary>
public sealed record B2BStatus
{
    public required string MerchantId { get; init; }

    /// <summary>"NotSubscribed" | "Subscribed" — this project's own status label, not an assumed real API value.</summary>
    public required string Status { get; init; }
}
