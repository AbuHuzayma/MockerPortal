namespace Portal.Application.Merchants;

/// <summary>The updatable Merchant field whitelist — master spec §16.</summary>
public sealed record MerchantFieldDefinition(string Key, string ColumnName, string Label);

public static class MerchantFieldCatalog
{
    public static IReadOnlyList<MerchantFieldDefinition> Fields { get; } =
    [
        new("nameEn", "NAME_EN", "Name (English)"),
        new("nameAr", "NAME_AR", "Name (Arabic)"),
        new("brandNameEn", "BRAND_NAME_EN", "Brand Name (English)"),
        new("brandNameAr", "BRAND_NAME_AR", "Brand Name (Arabic)"),
        new("crExpiryDate", "CR_EXPIRY_DATE", "CR Expiry Date"),
        new("idExpiryDate", "ID_EXPIRY_DATE", "ID Expiry Date"),
    ];

    public static readonly IReadOnlySet<string> ValidKeys = Fields.Select(f => f.Key).ToHashSet();
}
