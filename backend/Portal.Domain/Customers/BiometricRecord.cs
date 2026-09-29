namespace Portal.Domain.Customers;

/// <summary>
/// Biometric expiry fields for one customer — docs/05 §3 marks BiometricDatabase
/// entirely unknown. Mirrors IvrRecord: a bag keyed by BiometricFieldCatalog,
/// currently empty.
/// </summary>
public sealed record BiometricRecord
{
    public required string CustId { get; init; }
    public required string CustNumber { get; init; }
    public required IReadOnlyDictionary<string, object?> Fields { get; init; }
}
