namespace Portal.Domain.Merchants;

/// <summary>
/// Master spec §16. MerchantId/MerchantNumber are not among the spec's listed
/// fields (NAME_EN, NAME_AR, BRAND_NAME_EN, BRAND_NAME_AR, CR_EXPIRY_DATE,
/// ID_EXPIRY_DATE) — they're this project's own identifier convention,
/// mirroring CUST_ID/CUST_NUMBER for Customer, since every entity needs a
/// primary key and none was given for Merchant.
/// </summary>
public sealed class Merchant
{
    public required string MerchantId { get; init; }
    public required string MerchantNumber { get; init; }
    public string? NameEn { get; init; }
    public string? NameAr { get; init; }
    public string? BrandNameEn { get; init; }
    public string? BrandNameAr { get; init; }
    public string? CrExpiryDate { get; init; }
    public string? IdExpiryDate { get; init; }
}
