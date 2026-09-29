using Moq;
using Portal.Application.Customers;
using Portal.Domain.Customers;
using Xunit;

namespace Portal.UnitTests.Customers;

public class IvrServiceTests
{
    private static IvrRecord SampleRecord(string custId = "CUST-1") => new()
    {
        CustId = custId,
        CustNumber = "1",
        Fields = new Dictionary<string, object?>(),
    };

    [Fact]
    public async Task GetIvrAsync_returns_not_found_when_provider_returns_null()
    {
        var provider = new Mock<IIvrProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IvrRecord?)null);
        var service = new IvrService(provider.Object);

        var result = await service.GetIvrAsync("CUST-999", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.CustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task UpdateIvrAsync_filters_out_every_field_since_the_catalog_is_empty()
    {
        var provider = new Mock<IIvrProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync("CUST-1", It.IsAny<CancellationToken>())).ReturnsAsync(SampleRecord());
        IReadOnlyDictionary<string, object?>? capturedFields = null;
        provider.Setup(p => p.UpdateAsync("CUST-1", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, object?>, CancellationToken>((_, fields, _) => capturedFields = fields)
            .ReturnsAsync(SampleRecord());
        var service = new IvrService(provider.Object);

        await service.UpdateIvrAsync("CUST-1", new Dictionary<string, object?> { ["anything"] = "value" }, CancellationToken.None);

        Assert.NotNull(capturedFields);
        Assert.Empty(capturedFields!);
    }

    [Fact]
    public async Task UpdateIvrAsync_returns_not_found_when_customer_does_not_exist()
    {
        var provider = new Mock<IIvrProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IvrRecord?)null);
        var service = new IvrService(provider.Object);

        var result = await service.UpdateIvrAsync("CUST-999", new Dictionary<string, object?>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.CustomerNotFound, result.ErrorCode);
    }
}
