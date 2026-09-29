using Portal.Infrastructure.Customers;
using Xunit;

namespace Portal.IntegrationTests.Customers;

public class MockCreationProviderTests
{
    private readonly MockCreationProvider _provider = new();

    [Fact]
    public async Task GetByCustomerIdAsync_matches_either_CustId_or_CustNumber()
    {
        var byId = await _provider.GetByCustomerIdAsync("CUST-100001", CancellationToken.None);
        var byNumber = await _provider.GetByCustomerIdAsync("100001", CancellationToken.None);

        Assert.NotNull(byId);
        Assert.NotNull(byNumber);
        Assert.Equal(byId!.CustId, byNumber!.CustId);
        Assert.Equal("2022-03-10", byId.Fields["createdDate"]);
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
        await _provider.UpdateAsync("CUST-100002", new Dictionary<string, object?> { ["dateOfBirth"] = "1999-05-05" }, CancellationToken.None);

        var reread = await _provider.GetByCustomerIdAsync("CUST-100002", CancellationToken.None);

        Assert.Equal("1999-05-05", reread!.Fields["dateOfBirth"]);
        Assert.Equal("2023-01-05", reread.Fields["createdDate"]); // untouched field survives the merge
    }
}
