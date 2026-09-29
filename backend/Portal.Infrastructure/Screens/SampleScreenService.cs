using Portal.Application.Screens;

namespace Portal.Infrastructure.Screens;

/// <summary>
/// In-memory, single-record, process-lifetime store — deliberately not persisted
/// anywhere (registered as a singleton). This is Phase 3 proof-of-concept data,
/// not a real screen; real screens persist through a named command against a
/// real provider (docs/07-dynamic-screen-engine.md §3), starting Phase 4.
/// </summary>
public sealed class SampleScreenService : ISampleScreenService
{
    private SampleRecord _record = new()
    {
        RecordId = "SAMPLE-0001",
        FullName = "Jordan Example",
        Age = 34,
        Balance = 1250.50m,
        JoinDate = new DateOnly(2024, 1, 15),
        IsActive = true,
        Status = "Active",
        Notes = "Seeded sample record — edit and save to see the round trip.",
        InternalNote = "Visible to Administrators only, via ScreenField.Permission.",
    };

    public Task<SampleRecord> GetRecordAsync(CancellationToken ct) => Task.FromResult(_record);

    public Task<SampleRecord> SaveAsync(SampleRecord record, CancellationToken ct)
    {
        _record = record with { RecordId = _record.RecordId };
        return Task.FromResult(_record);
    }
}
