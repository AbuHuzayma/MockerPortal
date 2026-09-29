using Moq;
using Portal.Application.Customers;
using Portal.Domain.Customers;
using Xunit;

namespace Portal.UnitTests.Customers;

public class CustomerServiceTests
{
    private static Customer SampleCustomer => new()
    {
        CustId = "CUST-1",
        CustNumber = "1",
        MobileNo = "0500000001",
    };

    [Fact]
    public async Task SearchByMobileNumberAsync_returns_success_when_provider_finds_a_match()
    {
        var provider = new Mock<ICustomerProvider>();
        provider.Setup(p => p.FindByMobileNumberAsync("0500000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleCustomer);
        var service = new CustomerService(provider.Object);

        var result = await service.SearchByMobileNumberAsync("0500000001", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("CUST-1", result.Value!.CustId);
    }

    [Fact]
    public async Task SearchByMobileNumberAsync_returns_customer_not_found_when_provider_returns_null()
    {
        var provider = new Mock<ICustomerProvider>();
        provider.Setup(p => p.FindByMobileNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        var service = new CustomerService(provider.Object);

        var result = await service.SearchByMobileNumberAsync("0500000099", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.CustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetProfileAsync_returns_customer_not_found_when_provider_returns_null()
    {
        var provider = new Mock<ICustomerProvider>();
        provider.Setup(p => p.FindByCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        var service = new CustomerService(provider.Object);

        var result = await service.GetProfileAsync("CUST-999", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.CustomerNotFound, result.ErrorCode);
    }
}
