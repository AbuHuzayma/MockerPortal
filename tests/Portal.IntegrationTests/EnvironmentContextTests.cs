using Microsoft.Extensions.Configuration;
using Portal.Infrastructure.Common;
using Xunit;

namespace Portal.IntegrationTests;

public class EnvironmentContextTests
{
    [Theory]
    [InlineData("QA", "#0288D1")]
    [InlineData("PREPROD", "#ED6C02")]
    [InlineData("DEV", "#6B6572")]
    public void ToPublicInfo_maps_known_environment_to_its_badge_color(string environmentName, string expectedColor)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Environment:Name"] = environmentName })
            .Build();

        var context = new EnvironmentContext(configuration);

        var info = context.ToPublicInfo();

        Assert.Equal(environmentName, info.Name);
        Assert.Equal(expectedColor, info.Color);
    }

    [Fact]
    public void Name_defaults_to_DEV_when_not_configured()
    {
        var configuration = new ConfigurationBuilder().Build();

        var context = new EnvironmentContext(configuration);

        Assert.Equal("DEV", context.Name);
    }
}
