using Portal.Infrastructure.Customers;
using Xunit;

namespace Portal.IntegrationTests.Customers;

public class MockCustomerProviderTests
{
    private readonly MockCustomerProvider _provider = new();

    [Fact]
    public async Task FindByMobileNumberAsync_finds_a_seeded_fixture()
    {
        var customer = await _provider.FindByMobileNumberAsync("0500000001", CancellationToken.None);

        Assert.NotNull(customer);
        Assert.Equal("CUST-100001", customer!.CustId);
    }

    [Fact]
    public async Task FindByMobileNumberAsync_returns_null_for_an_unknown_number()
    {
        var customer = await _provider.FindByMobileNumberAsync("0599999999", CancellationToken.None);

        Assert.Null(customer);
    }

    [Theory]
    [InlineData("CUST-100002")]
    [InlineData("100002")]
    public async Task FindByCustomerIdAsync_matches_either_CustId_or_CustNumber(string identifier)
    {
        var customer = await _provider.FindByCustomerIdAsync(identifier, CancellationToken.None);

        Assert.NotNull(customer);
        Assert.Equal("CUST-100002", customer!.CustId);
    }
}
