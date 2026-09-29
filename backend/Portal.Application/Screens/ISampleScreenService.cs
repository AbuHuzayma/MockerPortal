namespace Portal.Application.Screens;

public interface ISampleScreenService
{
    Task<SampleRecord> GetRecordAsync(CancellationToken ct);

    /// <summary>Applies the change and returns the new state — the controller writes the audit entry.</summary>
    Task<SampleRecord> SaveAsync(SampleRecord record, CancellationToken ct);
}
