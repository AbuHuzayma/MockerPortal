using Moq;
using Portal.Application.Customers;
using Portal.Domain.Customers;
using Xunit;

namespace Portal.UnitTests.Customers;

public class CreationServiceTests
{
    private static CreationRecord SampleRecord(string custId = "CUST-1") => new()
    {
        CustId = custId,
        CustNumber = "1",
        Fields = new Dictionary<string, object?> { ["createdDate"] = "2022-01-01" },
    };

    [Fact]
    public async Task GetCreationAsync_returns_not_found_when_provider_returns_null()
    {
        var provider = new Mock<ICreationProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreationRecord?)null);
        var service = new CreationService(provider.Object);

        var result = await service.GetCreationAsync("CUST-999", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.CustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task UpdateCreationAsync_only_forwards_whitelisted_keys_to_the_provider()
    {
        var provider = new Mock<ICreationProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync("CUST-1", It.IsAny<CancellationToken>())).ReturnsAsync(SampleRecord());
        IReadOnlyDictionary<string, object?>? capturedFields = null;
        provider.Setup(p => p.UpdateAsync("CUST-1", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, object?>, CancellationToken>((_, fields, _) => capturedFields = fields)
            .ReturnsAsync(SampleRecord());
        var service = new CreationService(provider.Object);

        var requested = new Dictionary<string, object?>
        {
            ["dateOfBirth"] = "1990-01-01",
            ["notAKnownField"] = "should be dropped",
        };

        await service.UpdateCreationAsync("CUST-1", requested, CancellationToken.None);

        Assert.NotNull(capturedFields);
        Assert.True(capturedFields!.ContainsKey("dateOfBirth"));
        Assert.False(capturedFields.ContainsKey("notAKnownField"));
    }

    [Fact]
    public async Task UpdateCreationAsync_returns_not_found_when_customer_does_not_exist()
    {
        var provider = new Mock<ICreationProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreationRecord?)null);
        var service = new CreationService(provider.Object);

        var result = await service.UpdateCreationAsync("CUST-999", new Dictionary<string, object?>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.CustomerNotFound, result.ErrorCode);
    }
}
