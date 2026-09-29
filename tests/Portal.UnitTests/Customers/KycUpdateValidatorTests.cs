using Portal.Application.Customers;
using Xunit;

namespace Portal.UnitTests.Customers;

public class KycUpdateValidatorTests
{
    private readonly KycUpdateValidator _validator = new();

    [Fact]
    public async Task Accepts_a_valid_email()
    {
        var result = await _validator.ValidateAsync(new KycFieldValues(new Dictionary<string, object?> { ["email"] = "a@b.com" }));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_a_malformed_email()
    {
        var result = await _validator.ValidateAsync(new KycFieldValues(new Dictionary<string, object?> { ["email"] = "not-an-email" }));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Accepts_a_payload_with_no_email_field_at_all()
    {
        var result = await _validator.ValidateAsync(new KycFieldValues(new Dictionary<string, object?> { ["firstName"] = "Sara" }));

        Assert.True(result.IsValid);
    }
}
