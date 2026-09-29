using Portal.Infrastructure.Customers;
using Xunit;

namespace Portal.IntegrationTests.Customers;

public class MockKycProviderTests
{
    private readonly MockKycProvider _provider = new();

    [Fact]
    public async Task GetByCustomerIdAsync_matches_either_CustId_or_CustNumber()
    {
        var byId = await _provider.GetByCustomerIdAsync("CUST-100001", CancellationToken.None);
        var byNumber = await _provider.GetByCustomerIdAsync("100001", CancellationToken.None);

        Assert.NotNull(byId);
        Assert.NotNull(byNumber);
        Assert.Equal(byId!.CustId, byNumber!.CustId);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_returns_null_for_an_unknown_customer()
    {
        var record = await _provider.GetByCustomerIdAsync("CUST-DOES-NOT-EXIST", CancellationToken.None);

        Assert.Null(record);
    }

    [Fact]
    public async Task UpdateAsync_merges_changes_and_persists_them_for_the_next_read()
    {
        await _provider.UpdateAsync("CUST-100002", new Dictionary<string, object?> { ["firstName"] = "Changed" }, CancellationToken.None);

        var reread = await _provider.GetByCustomerIdAsync("CUST-100002", CancellationToken.None);

        Assert.Equal("Changed", reread!.Fields["firstName"]);
        // Untouched fields survive the merge.
        Assert.Equal("0500000002", reread.Fields["mobileNo"]);
    }
}
