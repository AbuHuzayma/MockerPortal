namespace Portal.Domain.Customers;

/// <summary>Master spec §14 — Card Creation/Activation via an internal Card Management System API.</summary>
public sealed record CardStatus
{
    public required string CustomerId { get; init; }
    public string? CardNumber { get; init; }

    /// <summary>"None" | "Created" | "Active" — this project's own status label, not an assumed real API value.</summary>
    public required string Status { get; init; }
}
