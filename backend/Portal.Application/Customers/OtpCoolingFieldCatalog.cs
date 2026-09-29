namespace Portal.Application.Customers;

/// <summary>
/// Deliberately empty — master spec §13 gives no field names for the OTP/IVR
/// cooling period screen, only "the backend must encapsulate the MSSQL
/// implementation." Same rationale as IvrFieldCatalog.
/// </summary>
public sealed record OtpCoolingFieldDefinition(string Key, string ColumnName, string Label, bool IsBoolean = false);

public static class OtpCoolingFieldCatalog
{
    public static IReadOnlyList<OtpCoolingFieldDefinition> Fields { get; } = [];

    public static readonly IReadOnlySet<string> ValidKeys = Fields.Select(f => f.Key).ToHashSet();
}
