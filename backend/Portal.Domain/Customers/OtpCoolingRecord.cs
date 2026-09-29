namespace Portal.Domain.Customers;

/// <summary>
/// OTP/IVR cooling period fields for one customer — master spec §13 gives no
/// field names ("the backend must encapsulate the MSSQL implementation"), so
/// this mirrors IvrRecord: a bag keyed by OtpCoolingFieldCatalog, currently empty.
/// </summary>
public sealed record OtpCoolingRecord
{
    public required string CustId { get; init; }
    public required string CustNumber { get; init; }
    public required IReadOnlyDictionary<string, object?> Fields { get; init; }
}
