namespace Portal.Application.Customers;

/// <summary>
/// Deliberately empty — the Biometric Authentication Module's database schema
/// is unknown (docs/05-database-design.md §3, master spec §19). Same
/// rationale as IvrFieldCatalog.
/// </summary>
public sealed record BiometricFieldDefinition(string Key, string ColumnName, string Label, bool IsBoolean = false);

public static class BiometricFieldCatalog
{
    public static IReadOnlyList<BiometricFieldDefinition> Fields { get; } = [];

    public static readonly IReadOnlySet<string> ValidKeys = Fields.Select(f => f.Key).ToHashSet();
}
