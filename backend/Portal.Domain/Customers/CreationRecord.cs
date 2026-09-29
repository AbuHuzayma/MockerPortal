namespace Portal.Domain.Customers;

/// <summary>
/// Customer creation date fields for one customer, keyed by the field keys
/// enumerated in Portal.Application.Customers.CreationFieldCatalog — master
/// spec §12. See KycFieldCatalog's doc comment for why this is a bag rather
/// than a POCO with named properties.
/// </summary>
public sealed record CreationRecord
{
    public required string CustId { get; init; }
    public required string CustNumber { get; init; }
    public required IReadOnlyDictionary<string, object?> Fields { get; init; }
}
