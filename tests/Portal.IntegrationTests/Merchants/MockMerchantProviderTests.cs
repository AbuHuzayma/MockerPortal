using Portal.Infrastructure.Merchants;
using Xunit;

namespace Portal.IntegrationTests.Merchants;

public class MockMerchantProviderTests
{
    private readonly MockMerchantProvider _provider = new();

    [Fact]
    public async Task SearchByNameAsync_matches_a_partial_english_name()
    {
        var results = await _provider.SearchByNameAsync("Falcon", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("MERCH-200002", results[0].MerchantId);
    }

    [Fact]
    public async Task FindByMerchantIdAsync_matches_either_MerchantId_or_MerchantNumber()
    {
        var byId = await _provider.FindByMerchantIdAsync("MERCH-200001", CancellationToken.None);
        var byNumber = await _provider.FindByMerchantIdAsync("200001", CancellationToken.None);

        Assert.NotNull(byId);
        Assert.NotNull(byNumber);
        Assert.Equal(byId!.MerchantId, byNumber!.MerchantId);
    }

    [Fact]
    public async Task UpdateAsync_merges_changes_and_persists_them_for_the_next_read()
    {
        await _provider.UpdateAsync("MERCH-200001", new Dictionary<string, object?> { ["nameEn"] = "Renamed" }, CancellationToken.None);

        var reread = await _provider.FindByMerchantIdAsync("MERCH-200001", CancellationToken.None);

        Assert.Equal("Renamed", reread!.NameEn);
        Assert.Equal("النور للتجارة", reread.NameAr); // untouched field survives the merge
    }
}
