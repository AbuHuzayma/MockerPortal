namespace Portal.Domain.Customers;

/// <summary>Master spec §21 — read-only. Fields: CUST_ID, CUST_NUMBER, T24_CUSTOMER_ID, DATE_CREATED.</summary>
public sealed record OnboardingInfo
{
    public required string CustId { get; init; }
    public required string CustNumber { get; init; }
    public string? T24CustomerId { get; init; }
    public string? DateCreated { get; init; }
}
