using FluentValidation;

namespace Portal.Application.Screens;

/// <summary>
/// Backing data for the SAMPLE_SCREEN proof-of-concept — not a real business
/// entity. Field names match ScreenField.IntegrationKey values in ScreenSeeder.
/// </summary>
public sealed record SampleRecord
{
    public required string RecordId { get; init; }
    public required string FullName { get; init; }
    public int? Age { get; init; }
    public decimal? Balance { get; init; }
    public DateOnly? JoinDate { get; init; }
    public bool IsActive { get; init; }
    public required string Status { get; init; }
    public string? Notes { get; init; }
    public string? InternalNote { get; init; }
}

public sealed class SampleRecordValidator : AbstractValidator<SampleRecord>
{
    private static readonly string[] AllowedStatuses = ["Active", "Inactive", "Pending"];

    public SampleRecordValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Age).InclusiveBetween(0, 150).When(x => x.Age.HasValue);
        RuleFor(x => x.Balance).GreaterThanOrEqualTo(0).When(x => x.Balance.HasValue);
        RuleFor(x => x.Status).NotEmpty().Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");
    }
}
