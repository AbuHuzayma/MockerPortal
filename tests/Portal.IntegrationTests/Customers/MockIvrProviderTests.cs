using Portal.Infrastructure.Customers;
using Xunit;

namespace Portal.IntegrationTests.Customers;

public class MockIvrProviderTests
{
    private readonly MockIvrProvider _provider = new();

    [Fact]
    public async Task GetByCustomerIdAsync_matches_either_CustId_or_CustNumber()
    {
        var byId = await _provider.GetByCustomerIdAsync("CUST-100001", CancellationToken.None);
        var byNumber = await _provider.GetByCustomerIdAsync("100001", CancellationToken.None);

        Assert.NotNull(byId);
        Assert.NotNull(byNumber);
        Assert.Equal(byId!.CustId, byNumber!.CustId);
        Assert.Empty(byId.Fields);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_returns_null_for_an_unknown_customer()
    {
        var record = await _provider.GetByCustomerIdAsync("CUST-DOES-NOT-EXIST", CancellationToken.None);

        Assert.Null(record);
    }
}
