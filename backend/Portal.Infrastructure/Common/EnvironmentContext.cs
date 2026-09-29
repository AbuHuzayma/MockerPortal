using Microsoft.Extensions.Configuration;
using Portal.Application.Common;

namespace Portal.Infrastructure.Common;

public sealed class EnvironmentContext : IEnvironmentContext
{
    private static readonly Dictionary<string, string> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DEV"] = "#6B6572",
        ["QA"] = "#0288D1",
        ["PREPROD"] = "#ED6C02"
    };

    public EnvironmentContext(IConfiguration configuration)
    {
        Name = configuration["Environment:Name"] ?? "DEV";
    }

    public string Name { get; }

    public EnvironmentInfo ToPublicInfo() =>
        new(Name, Name, Colors.GetValueOrDefault(Name, "#6B6572"));
}
