namespace Portal.Domain.Customers;

/// <summary>Master spec §15 — internal transfer beneficiary activation via an internal Beneficiary Service API.</summary>
public sealed record BeneficiaryStatus
{
    public required string CustomerId { get; init; }
    public required string BeneficiaryId { get; init; }

    /// <summary>"Pending" | "Active" — this project's own status label, not an assumed real API value.</summary>
    public required string Status { get; init; }
}
