namespace Portal.Domain.Customers;

/// <summary>
/// KYC field data for one customer, keyed by the field keys enumerated in
/// Portal.Application.Customers.KycFieldCatalog. Values are <c>string?</c>
/// except the PEP sub-flags, which are <c>bool</c>. See KycFieldCatalog's
/// doc comment for why this is a bag rather than ~68 POCO properties.
/// </summary>
public sealed record KycRecord
{
    public required string CustId { get; init; }
    public required string CustNumber { get; init; }
    public required IReadOnlyDictionary<string, object?> Fields { get; init; }

    /// <summary>PEP is derived from the four PEP_BY_* flags (master spec §10), never stored independently.</summary>
    public bool ComputePep() =>
        IsTrue("pepByScreening") || IsTrue("pepByCustomer") || IsTrue("pepByProfession") || IsTrue("pepByRelationship");

    private bool IsTrue(string key) => Fields.TryGetValue(key, out var value) && value is true;
}
