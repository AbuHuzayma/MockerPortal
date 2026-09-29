using Portal.Application.Screens;
using Xunit;

namespace Portal.UnitTests.Screens;

public class SampleRecordValidatorTests
{
    private readonly SampleRecordValidator _validator = new();

    private static SampleRecord ValidRecord => new()
    {
        RecordId = "SAMPLE-0001",
        FullName = "Jordan Example",
        Age = 34,
        Status = "Active",
    };

    [Fact]
    public async Task Accepts_a_well_formed_record()
    {
        var result = await _validator.ValidateAsync(ValidRecord);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_an_empty_full_name()
    {
        var result = await _validator.ValidateAsync(ValidRecord with { FullName = "" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Rejects_an_out_of_range_age()
    {
        var result = await _validator.ValidateAsync(ValidRecord with { Age = 200 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Rejects_a_status_outside_the_allowed_set()
    {
        var result = await _validator.ValidateAsync(ValidRecord with { Status = "Deleted" });

        Assert.False(result.IsValid);
    }
}
