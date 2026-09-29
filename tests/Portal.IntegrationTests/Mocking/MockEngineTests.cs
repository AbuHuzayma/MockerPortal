using Microsoft.EntityFrameworkCore;
using Portal.Application.Common;
using Portal.Application.Mocking;
using Portal.Infrastructure.Common;
using Portal.Infrastructure.Mocking;
using Portal.Infrastructure.Persistence;
using Xunit;

namespace Portal.IntegrationTests.Mocking;

public class MockEngineTests
{
    private static PortalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase($"MockEngineTests-{Guid.NewGuid()}")
            .Options;
        return new PortalDbContext(options);
    }

    private static IEnvironmentContext Env(string name = "DEV") =>
        new StaticEnvironmentContext(name);

    private sealed class StaticEnvironmentContext(string name) : IEnvironmentContext
    {
        public string Name { get; } = name;
        public EnvironmentInfo ToPublicInfo() => new(Name, Name, "#000000");
    }

    private static MockRequestContext Request(
        string apiCode,
        string path,
        string method = "POST",
        Dictionary<string, string>? headers = null,
        Dictionary<string, string>? query = null,
        string? body = null) => new(apiCode, path, method, headers ?? [], query ?? [], body);

    private static async Task SeedAsync(PortalDbContext dbContext, MockApi api)
    {
        dbContext.MockApis.Add(api);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ResolveAsync_returns_null_when_no_active_MockApi_matches_the_code()
    {
        var dbContext = CreateContext();
        var engine = new MockEngine(dbContext, Env());

        var result = await engine.ResolveAsync(Request("UNKNOWN", "anything"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_returns_null_when_the_MockApi_is_inactive()
    {
        var dbContext = CreateContext();
        await SeedAsync(dbContext, new MockApi { Id = Guid.NewGuid(), Code = "ABSHER", Name = "Absher", Environment = "DEV", IsActive = false });
        var engine = new MockEngine(dbContext, Env());

        var result = await engine.ResolveAsync(Request("ABSHER", "verify"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_scopes_MockApis_by_environment()
    {
        var dbContext = CreateContext();
        await SeedAsync(dbContext, new MockApi
        {
            Id = Guid.NewGuid(),
            Code = "ABSHER",
            Name = "Absher",
            Environment = "QA",
            IsActive = true,
            Endpoints =
            [
                new MockEndpoint
                {
                    Id = Guid.NewGuid(),
                    Path = "verify",
                    HttpMethod = "POST",
                    IsActive = true,
                    Responses = [new MockResponse { Id = Guid.NewGuid(), Name = "Default", HttpStatusCode = 200, ResponseBody = "ok", IsActive = true, Priority = 1, MatchRules = [] }],
                },
            ],
        });
        var devEngine = new MockEngine(dbContext, Env("DEV"));
        var qaEngine = new MockEngine(dbContext, Env("QA"));

        Assert.Null(await devEngine.ResolveAsync(Request("ABSHER", "verify"), CancellationToken.None));
        Assert.NotNull(await qaEngine.ResolveAsync(Request("ABSHER", "verify"), CancellationToken.None));
    }

    [Fact]
    public async Task ResolveAsync_prefers_a_ruled_response_over_the_ruleless_default_regardless_of_priority()
    {
        var dbContext = CreateContext();
        await SeedAsync(dbContext, new MockApi
        {
            Id = Guid.NewGuid(),
            Code = "YAKEEN",
            Name = "Yakeen",
            Environment = "DEV",
            IsActive = true,
            Endpoints =
            [
                new MockEndpoint
                {
                    Id = Guid.NewGuid(),
                    Path = "verify",
                    HttpMethod = "POST",
                    IsActive = true,
                    Responses =
                    [
                        new MockResponse { Id = Guid.NewGuid(), Name = "Default", HttpStatusCode = 200, ResponseBody = "default", IsActive = true, Priority = 1, MatchRules = [] },
                        new MockResponse
                        {
                            Id = Guid.NewGuid(), Name = "Matched", HttpStatusCode = 201, ResponseBody = "matched", IsActive = true, Priority = 50,
                            MatchRules = [new MockMatchRule { Id = Guid.NewGuid(), Source = MockMatchSource.Header, Field = "X-Test-Case", Operator = MockMatchOperator.Equals, ExpectedValue = "MATCH" }],
                        },
                    ],
                },
            ],
        });
        var engine = new MockEngine(dbContext, Env());

        var result = await engine.ResolveAsync(
            Request("YAKEEN", "verify", headers: new() { ["X-Test-Case"] = "MATCH" }),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
        Assert.Equal("matched", result.Body);
    }

    [Theory]
    [InlineData(MockMatchOperator.Equals, "abc", "abc", true)]
    [InlineData(MockMatchOperator.Equals, "abc", "xyz", false)]
    [InlineData(MockMatchOperator.NotEquals, "abc", "xyz", true)]
    [InlineData(MockMatchOperator.NotEquals, "abc", "abc", false)]
    [InlineData(MockMatchOperator.Contains, "abcdef", "cde", true)]
    [InlineData(MockMatchOperator.Contains, "abcdef", "zzz", false)]
    [InlineData(MockMatchOperator.StartsWith, "abcdef", "abc", true)]
    [InlineData(MockMatchOperator.StartsWith, "abcdef", "def", false)]
    public async Task ResolveAsync_evaluates_each_operator_against_a_query_string_field(
        string op, string actualValue, string expectedValue, bool shouldMatch)
    {
        var dbContext = CreateContext();
        await SeedAsync(dbContext, new MockApi
        {
            Id = Guid.NewGuid(),
            Code = "ELM",
            Name = "ELM",
            Environment = "DEV",
            IsActive = true,
            Endpoints =
            [
                new MockEndpoint
                {
                    Id = Guid.NewGuid(),
                    Path = "cr",
                    HttpMethod = "GET",
                    IsActive = true,
                    Responses =
                    [
                        new MockResponse
                        {
                            Id = Guid.NewGuid(), Name = "Matched", HttpStatusCode = 200, ResponseBody = "matched", IsActive = true, Priority = 1,
                            MatchRules = [new MockMatchRule { Id = Guid.NewGuid(), Source = MockMatchSource.QueryString, Field = "value", Operator = op, ExpectedValue = expectedValue }],
                        },
                    ],
                },
            ],
        });
        var engine = new MockEngine(dbContext, Env());

        var result = await engine.ResolveAsync(
            Request("ELM", "cr", "GET", query: new() { ["value"] = actualValue }),
            CancellationToken.None);

        Assert.Equal(shouldMatch, result is not null);
    }

    [Fact]
    public async Task ResolveAsync_matches_a_flat_JSON_property_in_the_request_body()
    {
        var dbContext = CreateContext();
        await SeedAsync(dbContext, new MockApi
        {
            Id = Guid.NewGuid(),
            Code = "ELM",
            Name = "ELM",
            Environment = "DEV",
            IsActive = true,
            Endpoints =
            [
                new MockEndpoint
                {
                    Id = Guid.NewGuid(),
                    Path = "cr",
                    HttpMethod = "POST",
                    IsActive = true,
                    Responses =
                    [
                        new MockResponse
                        {
                            Id = Guid.NewGuid(), Name = "Matched", HttpStatusCode = 200, ResponseBody = "matched", IsActive = true, Priority = 1,
                            MatchRules = [new MockMatchRule { Id = Guid.NewGuid(), Source = MockMatchSource.RequestBody, Field = "crNumber", Operator = MockMatchOperator.Equals, ExpectedValue = "12345" }],
                        },
                    ],
                },
            ],
        });
        var engine = new MockEngine(dbContext, Env());

        var result = await engine.ResolveAsync(
            Request("ELM", "cr", body: """{"crNumber":"12345"}"""),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResolveAsync_returns_null_when_no_response_matches_and_there_is_no_default()
    {
        var dbContext = CreateContext();
        await SeedAsync(dbContext, new MockApi
        {
            Id = Guid.NewGuid(),
            Code = "ELM",
            Name = "ELM",
            Environment = "DEV",
            IsActive = true,
            Endpoints =
            [
                new MockEndpoint
                {
                    Id = Guid.NewGuid(),
                    Path = "cr",
                    HttpMethod = "POST",
                    IsActive = true,
                    Responses =
                    [
                        new MockResponse
                        {
                            Id = Guid.NewGuid(), Name = "Matched", HttpStatusCode = 200, ResponseBody = "matched", IsActive = true, Priority = 1,
                            MatchRules = [new MockMatchRule { Id = Guid.NewGuid(), Source = MockMatchSource.Header, Field = "X-Test-Case", Operator = MockMatchOperator.Equals, ExpectedValue = "NEVER" }],
                        },
                    ],
                },
            ],
        });
        var engine = new MockEngine(dbContext, Env());

        var result = await engine.ResolveAsync(Request("ELM", "cr"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyTemplate_substitutes_known_tokens_and_leaves_unknown_tokens_untouched()
    {
        var dbContext = CreateContext();
        await SeedAsync(dbContext, new MockApi
        {
            Id = Guid.NewGuid(),
            Code = "ABSHER",
            Name = "Absher",
            Environment = "DEV",
            IsActive = true,
            Endpoints =
            [
                new MockEndpoint
                {
                    Id = Guid.NewGuid(),
                    Path = "verify",
                    HttpMethod = "POST",
                    IsActive = true,
                    Responses =
                    [
                        new MockResponse
                        {
                            Id = Guid.NewGuid(), Name = "Default", HttpStatusCode = 200, IsActive = true, Priority = 1, MatchRules = [],
                            ResponseBody = "{{request.header.X-Correlation-Id}}|{{request.query.nationalId}}|{{request.body.foo}}|{{unknown.token}}",
                        },
                    ],
                },
            ],
        });
        var engine = new MockEngine(dbContext, Env());

        var result = await engine.ResolveAsync(
            Request(
                "ABSHER",
                "verify",
                headers: new() { ["X-Correlation-Id"] = "corr-1" },
                query: new() { ["nationalId"] = "1234567890" },
                body: """{"foo":"bar"}"""),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("corr-1|1234567890|bar|{{unknown.token}}", result!.Body);
    }

    [Fact]
    public async Task ApplyTemplate_now_and_uuid_tokens_produce_non_empty_distinct_values()
    {
        var dbContext = CreateContext();
        await SeedAsync(dbContext, new MockApi
        {
            Id = Guid.NewGuid(),
            Code = "ABSHER",
            Name = "Absher",
            Environment = "DEV",
            IsActive = true,
            Endpoints =
            [
                new MockEndpoint
                {
                    Id = Guid.NewGuid(),
                    Path = "verify",
                    HttpMethod = "POST",
                    IsActive = true,
                    Responses = [new MockResponse { Id = Guid.NewGuid(), Name = "Default", HttpStatusCode = 200, IsActive = true, Priority = 1, MatchRules = [], ResponseBody = "{{now}}|{{uuid}}" }],
                },
            ],
        });
        var engine = new MockEngine(dbContext, Env());

        var result = await engine.ResolveAsync(Request("ABSHER", "verify"), CancellationToken.None);

        var parts = result!.Body!.Split('|');
        Assert.True(DateTime.TryParse(parts[0], out _));
        Assert.True(Guid.TryParse(parts[1], out _));
    }
}
