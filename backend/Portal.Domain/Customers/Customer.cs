namespace Portal.Domain.Customers;

/// <summary>
/// Read-only customer identity/status summary, sourced from the enterprise
/// CustomerDatabase (T_PRT_CUSTOMER) via ICustomerProvider — never a portal-owned
/// table. Field names match the subset of docs/02-requirements.md's KYC field
/// list needed for a profile view; the full KYC field set is edited by the
/// dedicated KYC screen/command in Phase 4, not here.
/// </summary>
public sealed class Customer
{
    public required string CustId { get; init; }
    public required string CustNumber { get; init; }
    public string? T24CustomerId { get; init; }
    public required string MobileNo { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ArabicFirstName { get; init; }
    public string? ArabicLastName { get; init; }
    public string? Email { get; init; }
    public string? Nationality { get; init; }
    public string? LifeStatus { get; init; }
    public string? BlacklistStatus { get; init; }
}
