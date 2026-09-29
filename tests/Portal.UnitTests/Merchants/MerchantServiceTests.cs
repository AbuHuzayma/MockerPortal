using Moq;
using Portal.Application.Merchants;
using Portal.Domain.Merchants;
using Xunit;

namespace Portal.UnitTests.Merchants;

public class MerchantServiceTests
{
    private static Merchant SampleMerchant(string merchantId = "MERCH-1") => new()
    {
        MerchantId = merchantId,
        MerchantNumber = "1",
        NameEn = "Test Merchant",
    };

    [Fact]
    public async Task SearchByNameAsync_returns_not_found_when_no_matches()
    {
        var provider = new Mock<IMerchantProvider>();
        provider.Setup(p => p.SearchByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Merchant>)[]);
        var service = new MerchantService(provider.Object);

        var result = await service.SearchByNameAsync("nope", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MerchantErrorCodes.MerchantNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetProfileAsync_returns_not_found_when_provider_returns_null()
    {
        var provider = new Mock<IMerchantProvider>();
        provider.Setup(p => p.FindByMerchantIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Merchant?)null);
        var service = new MerchantService(provider.Object);

        var result = await service.GetProfileAsync("MERCH-999", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MerchantErrorCodes.MerchantNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_only_forwards_whitelisted_keys_to_the_provider()
    {
        var provider = new Mock<IMerchantProvider>();
        provider.Setup(p => p.FindByMerchantIdAsync("MERCH-1", It.IsAny<CancellationToken>())).ReturnsAsync(SampleMerchant());
        IReadOnlyDictionary<string, object?>? captured = null;
        provider.Setup(p => p.UpdateAsync("MERCH-1", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, object?>, CancellationToken>((_, fields, _) => captured = fields)
            .ReturnsAsync(SampleMerchant());
        var service = new MerchantService(provider.Object);

        await service.UpdateAsync("MERCH-1", new Dictionary<string, object?> { ["nameEn"] = "Updated", ["merchantId"] = "should be dropped" }, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.True(captured!.ContainsKey("nameEn"));
        Assert.False(captured.ContainsKey("merchantId"));
    }
}
