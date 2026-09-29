namespace Portal.Domain.Customers;

/// <summary>
/// IVR field data for one customer, keyed by the field keys enumerated in
/// Portal.Application.Customers.IvrFieldCatalog — currently empty, since the
/// master spec explicitly says "do not invent field names" for this screen
/// and the IVR database schema is unknown. See docs/05-database-design.md §3.
/// </summary>
public sealed record IvrRecord
{
    public required string CustId { get; init; }
    public required string CustNumber { get; init; }
    public required IReadOnlyDictionary<string, object?> Fields { get; init; }
}
