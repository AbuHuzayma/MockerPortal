using Portal.Application.Customers;
using Xunit;

namespace Portal.UnitTests.Customers;

public class CreationUpdateValidatorTests
{
    private readonly CreationUpdateValidator _validator = new();

    [Fact]
    public async Task Accepts_a_past_date_of_birth()
    {
        var result = await _validator.ValidateAsync(new CreationFieldValues(new Dictionary<string, object?> { ["dateOfBirth"] = "1990-01-01" }));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_a_future_date_of_birth()
    {
        var futureDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");

        var result = await _validator.ValidateAsync(new CreationFieldValues(new Dictionary<string, object?> { ["dateOfBirth"] = futureDate }));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Accepts_a_payload_with_no_date_of_birth_at_all()
    {
        var result = await _validator.ValidateAsync(new CreationFieldValues(new Dictionary<string, object?> { ["createdDate"] = "2024-01-01" }));

        Assert.True(result.IsValid);
    }
}
