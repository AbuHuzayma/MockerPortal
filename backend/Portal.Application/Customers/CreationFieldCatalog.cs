namespace Portal.Application.Customers;

/// <summary>
/// The Customer Creation Details field whitelist — master spec §12. Two of
/// the three fields have an ambiguous column name in the spec itself
/// ("BIRTH_DATE / CUST_BIRTH_DATE", "KYC_OK_DATE / KYC_LEVEL_DATE"); the
/// first-listed name is used as the primary column here — confirm against
/// the real T_PRT_CUSTOMER schema before relying on this in QA/PREPROD, and
/// swap ColumnName if the other name turns out to be correct (a one-line
/// change, same as adding any other field — see docs/07 §3).
/// </summary>
public sealed record CreationFieldDefinition(string Key, string ColumnName, string Label);

public static class CreationFieldCatalog
{
    public static IReadOnlyList<CreationFieldDefinition> Fields { get; } =
    [
        new("createdDate", "DATE_CREATED", "Created Date"),
        new("dateOfBirth", "BIRTH_DATE", "Date of Birth"),
        new("kycDate", "KYC_OK_DATE", "KYC Date"),
    ];

    public static readonly IReadOnlySet<string> ValidKeys = Fields.Select(f => f.Key).ToHashSet();
}
