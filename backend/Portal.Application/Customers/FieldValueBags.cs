namespace Portal.Application.Customers;

/// <summary>
/// Distinct per-screen subclasses of the same underlying shape so each
/// screen's <c>IValidator&lt;T&gt;</c> registers (and resolves) independently.
/// Registering two validators against the bare
/// <c>IReadOnlyDictionary&lt;string, object?&gt;</c> would silently collide —
/// .NET DI resolves a single <c>IValidator&lt;T&gt;</c> injection to whichever
/// validator for that exact T was registered last, so the "losing" screen's
/// validation rules would never run.
/// </summary>
public abstract class FieldValueBag : Dictionary<string, object?>
{
    protected FieldValueBag(IReadOnlyDictionary<string, object?> source) : base(source)
    {
    }
}

public sealed class KycFieldValues(IReadOnlyDictionary<string, object?> source) : FieldValueBag(source);

public sealed class CreationFieldValues(IReadOnlyDictionary<string, object?> source) : FieldValueBag(source);
