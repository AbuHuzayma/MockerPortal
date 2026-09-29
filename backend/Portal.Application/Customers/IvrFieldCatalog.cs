namespace Portal.Application.Customers;

/// <summary>
/// The IVR field whitelist — deliberately empty. The master spec (§11) states
/// the IVR database fields "must be supplied/configured based on the existing
/// MSSQL schema" and explicitly says "do not invent field names." Until real
/// schema is supplied, no field can be written (any incoming update is
/// filtered down to nothing — see IvrService.UpdateIvrAsync), which is the
/// correct behavior for data we have no basis to define.
///
/// To add a real field once schema is supplied: add a row here (same shape as
/// KycFieldCatalog) and a corresponding ScreenField in
/// Portal.Infrastructure.Screens.ScreenSeeder.SeedIvrScreenAsync.
/// </summary>
public sealed record IvrFieldDefinition(string Key, string ColumnName, string Label, bool IsBoolean = false);

public static class IvrFieldCatalog
{
    public static IReadOnlyList<IvrFieldDefinition> Fields { get; } = [];

    public static readonly IReadOnlySet<string> ValidKeys = Fields.Select(f => f.Key).ToHashSet();
}
