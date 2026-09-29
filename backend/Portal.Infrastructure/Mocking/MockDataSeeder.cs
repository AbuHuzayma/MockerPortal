using Microsoft.EntityFrameworkCore;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Mocking;

/// <summary>
/// Seeds starter MockApi/MockEndpoint/MockResponse/MockMatchRule rows for the
/// three external dependencies the master spec names (Absher, Yakeen, ELM) —
/// docs/09-api-mocker.md §8. Field names here are illustrative starter
/// scenarios, not a real Absher/Yakeen/ELM contract (none was supplied — see
/// docs/08-integrations.md §4); QA/Dev edit these via the API Mocker admin
/// UI to match whatever the real contract turns out to be. Seeded inactive
/// (IsActive = false) so nothing serves mock traffic until someone with
/// api-mocker.enable explicitly turns it on. Idempotent: runs every startup,
/// skips any MockApi code that already exists for that environment.
/// </summary>
public static class MockDataSeeder
{
    public static async Task SeedAllAsync(PortalDbContext dbContext, CancellationToken ct = default)
    {
        foreach (var environment in new[] { "DEV", "QA", "PREPROD" })
        {
            await SeedAbsherAsync(dbContext, environment, ct);
            await SeedYakeenAsync(dbContext, environment, ct);
            await SeedElmAsync(dbContext, environment, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task<MockApi?> GetOrCreateAsync(
        PortalDbContext dbContext, string code, string environment, string name, string description, CancellationToken ct)
    {
        var existing = await dbContext.MockApis
            .FirstOrDefaultAsync(a => a.Code == code && a.Environment == environment, ct);
        if (existing is not null)
        {
            return null; // already seeded — never overwrite admin-edited config on restart.
        }

        var api = new MockApi
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Environment = environment,
            IsActive = false,
        };
        dbContext.MockApis.Add(api);
        return api;
    }

    private static async Task SeedAbsherAsync(PortalDbContext dbContext, string environment, CancellationToken ct)
    {
        var api = await GetOrCreateAsync(dbContext, "ABSHER", environment, "Absher",
            "Absher identity/national-address verification (starter sample — confirm real contract before relying on this).", ct);
        if (api is null)
        {
            return;
        }

        api.Endpoints.Add(new MockEndpoint
        {
            Id = Guid.NewGuid(),
            Path = "verify/national-id",
            HttpMethod = "POST",
            IsActive = true,
            Responses =
            [
                NewResponse("Valid national ID", 200,
                    """{"nationalId":"{{request.body.nationalId}}","verified":true,"correlationId":"{{uuid}}","timestamp":"{{now}}"}""",
                    priority: 1,
                    rules: [NewRule(MockMatchSource.Header, "X-Test-Case", MockMatchOperator.Equals, "VALID")]),
                NewResponse("National ID not found", 404,
                    """{"code":"NATIONAL_ID_NOT_FOUND","message":"No matching national ID record.","correlationId":"{{uuid}}"}""",
                    priority: 2,
                    rules: [NewRule(MockMatchSource.Header, "X-Test-Case", MockMatchOperator.Equals, "NATIONAL_ID_NOT_FOUND")]),
                NewResponse("Default", 200,
                    """{"nationalId":"{{request.body.nationalId}}","verified":true,"correlationId":"{{uuid}}","timestamp":"{{now}}"}""",
                    priority: 99, rules: []),
            ],
        });
    }

    private static async Task SeedYakeenAsync(PortalDbContext dbContext, string environment, CancellationToken ct)
    {
        var api = await GetOrCreateAsync(dbContext, "YAKEEN", environment, "Yakeen",
            "Yakeen citizen/Iqama verification (starter sample — confirm real contract before relying on this).", ct);
        if (api is null)
        {
            return;
        }

        api.Endpoints.Add(new MockEndpoint
        {
            Id = Guid.NewGuid(),
            Path = "citizen/verify",
            HttpMethod = "POST",
            IsActive = true,
            Responses =
            [
                NewResponse("Match", 200,
                    """{"idNumber":"{{request.body.idNumber}}","nameMatch":true,"dateOfBirthMatch":true,"correlationId":"{{uuid}}"}""",
                    priority: 1,
                    rules: [NewRule(MockMatchSource.Header, "X-Test-Case", MockMatchOperator.Equals, "MATCH")]),
                NewResponse("Mismatch", 200,
                    """{"idNumber":"{{request.body.idNumber}}","nameMatch":false,"dateOfBirthMatch":false,"correlationId":"{{uuid}}"}""",
                    priority: 2,
                    rules: [NewRule(MockMatchSource.Header, "X-Test-Case", MockMatchOperator.Equals, "MISMATCH")]),
                NewResponse("Default", 200,
                    """{"idNumber":"{{request.body.idNumber}}","nameMatch":true,"dateOfBirthMatch":true,"correlationId":"{{uuid}}"}""",
                    priority: 99, rules: []),
            ],
        });
    }

    private static async Task SeedElmAsync(PortalDbContext dbContext, string environment, CancellationToken ct)
    {
        var api = await GetOrCreateAsync(dbContext, "ELM", environment, "ELM",
            "ELM commercial registration / business verification (starter sample — confirm real contract before relying on this).", ct);
        if (api is null)
        {
            return;
        }

        api.Endpoints.Add(new MockEndpoint
        {
            Id = Guid.NewGuid(),
            Path = "cr/verify",
            HttpMethod = "POST",
            IsActive = true,
            Responses =
            [
                NewResponse("Active CR", 200,
                    """{"crNumber":"{{request.body.crNumber}}","status":"ACTIVE","correlationId":"{{uuid}}"}""",
                    priority: 1,
                    rules: [NewRule(MockMatchSource.Header, "X-Test-Case", MockMatchOperator.Equals, "ACTIVE")]),
                NewResponse("Expired CR", 200,
                    """{"crNumber":"{{request.body.crNumber}}","status":"EXPIRED","correlationId":"{{uuid}}"}""",
                    priority: 2,
                    rules: [NewRule(MockMatchSource.Header, "X-Test-Case", MockMatchOperator.Equals, "EXPIRED")]),
                NewResponse("Default", 200,
                    """{"crNumber":"{{request.body.crNumber}}","status":"ACTIVE","correlationId":"{{uuid}}"}""",
                    priority: 99, rules: []),
            ],
        });
    }

    private static MockResponse NewResponse(string name, int statusCode, string body, int priority, List<MockMatchRule> rules) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        HttpStatusCode = statusCode,
        ResponseHeaders = """{"Content-Type":"application/json"}""",
        ResponseBody = body,
        DelayMilliseconds = 0,
        IsActive = true,
        Priority = priority,
        MatchRules = rules,
    };

    private static MockMatchRule NewRule(string source, string field, string op, string expected) => new()
    {
        Id = Guid.NewGuid(),
        Source = source,
        Field = field,
        Operator = op,
        ExpectedValue = expected,
    };
}
