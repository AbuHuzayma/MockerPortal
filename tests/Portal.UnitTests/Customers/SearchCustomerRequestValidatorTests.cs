using Portal.Application.Customers;
using Xunit;

namespace Portal.UnitTests.Customers;

public class SearchCustomerRequestValidatorTests
{
    private readonly SearchCustomerRequestValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("abc1234567")]
    [InlineData("123")]
    public async Task Rejects_invalid_mobile_numbers(string mobileNumber)
    {
        var result = await _validator.ValidateAsync(new SearchCustomerRequest(mobileNumber));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Accepts_a_well_formed_mobile_number()
    {
        var result = await _validator.ValidateAsync(new SearchCustomerRequest("0500000001"));

        Assert.True(result.IsValid);
    }
}
