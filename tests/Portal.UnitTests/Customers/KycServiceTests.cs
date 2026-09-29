using Moq;
using Portal.Application.Customers;
using Portal.Domain.Customers;
using Xunit;

namespace Portal.UnitTests.Customers;

public class KycServiceTests
{
    private static KycRecord SampleRecord(string custId = "CUST-1") => new()
    {
        CustId = custId,
        CustNumber = "1",
        Fields = new Dictionary<string, object?>
        {
            ["mobileNo"] = "0500000001",
            ["firstName"] = "Sara",
            ["pepByScreening"] = false,
            ["pepByCustomer"] = false,
            ["pepByProfession"] = false,
            ["pepByRelationship"] = false,
        },
    };

    [Fact]
    public async Task GetKycAsync_returns_not_found_when_provider_returns_null()
    {
        var provider = new Mock<IKycProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycRecord?)null);
        var service = new KycService(provider.Object);

        var result = await service.GetKycAsync("CUST-999", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.CustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetKycAsync_injects_a_computed_pep_field()
    {
        var provider = new Mock<IKycProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync("CUST-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleRecord() with
            {
                Fields = new Dictionary<string, object?> { ["pepByScreening"] = true, ["pepByCustomer"] = false, ["pepByProfession"] = false, ["pepByRelationship"] = false },
            });
        var service = new KycService(provider.Object);

        var result = await service.GetKycAsync("CUST-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(true, result.Value!.Fields["pep"]);
    }

    [Fact]
    public async Task UpdateKycAsync_only_forwards_whitelisted_keys_to_the_provider()
    {
        var provider = new Mock<IKycProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync("CUST-1", It.IsAny<CancellationToken>())).ReturnsAsync(SampleRecord());
        IReadOnlyDictionary<string, object?>? capturedFields = null;
        provider.Setup(p => p.UpdateAsync("CUST-1", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, object?>, CancellationToken>((_, fields, _) => capturedFields = fields)
            .ReturnsAsync(SampleRecord());
        var service = new KycService(provider.Object);

        var requested = new Dictionary<string, object?>
        {
            ["firstName"] = "Updated",
            ["notAKnownField"] = "should be dropped",
            ["custId"] = "should also be dropped — identifiers aren't writable fields",
        };

        await service.UpdateKycAsync("CUST-1", requested, CancellationToken.None);

        Assert.NotNull(capturedFields);
        Assert.True(capturedFields!.ContainsKey("firstName"));
        Assert.False(capturedFields.ContainsKey("notAKnownField"));
        Assert.False(capturedFields.ContainsKey("custId"));
    }

    [Fact]
    public async Task UpdateKycAsync_returns_not_found_when_customer_does_not_exist()
    {
        var provider = new Mock<IKycProvider>();
        provider.Setup(p => p.GetByCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycRecord?)null);
        var service = new KycService(provider.Object);

        var result = await service.UpdateKycAsync("CUST-999", new Dictionary<string, object?>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrorCodes.CustomerNotFound, result.ErrorCode);
    }
}
