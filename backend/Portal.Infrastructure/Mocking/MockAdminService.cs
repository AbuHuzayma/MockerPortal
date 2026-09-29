using Microsoft.EntityFrameworkCore;
using Portal.Application.Common;
using Portal.Application.Mocking;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Mocking;

public sealed class MockAdminService(PortalDbContext dbContext, IEnvironmentContext environmentContext) : IMockAdminService
{
    // Scoped to the CURRENT deployed environment, same as IMockEngine.ResolveAsync
    // (docs/09-api-mocker.md §2: "MockApi ... scoped to an environment"). Each
    // environment is its own deployment, so Code is only unique within it — without
    // this filter, a shared dev database seeded with the same code across
    // environments (see MockDataSeeder) makes SingleOrDefaultAsync throw.
    private IQueryable<MockApi> FullTree() => dbContext.MockApis
        .Include(a => a.Endpoints).ThenInclude(e => e.Responses).ThenInclude(r => r.MatchRules)
        .Where(a => a.Environment == environmentContext.Name);

    public async Task<IReadOnlyList<MockApiDto>> ListAsync(CancellationToken ct) =>
        (await FullTree().OrderBy(a => a.Code).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<Result<MockApiDto>> GetAsync(string code, CancellationToken ct)
    {
        var api = await FullTree().SingleOrDefaultAsync(a => a.Code == code, ct);
        return api is null
            ? Result<MockApiDto>.Failure(MockErrorCodes.MockApiNotFound, "Mock API was not found.")
            : Result<MockApiDto>.Success(ToDto(api));
    }

    public async Task<MockApiDto> UpsertAsync(MockApiDto dto, CancellationToken ct)
    {
        var existing = await FullTree().SingleOrDefaultAsync(a => a.Code == dto.Code, ct);

        if (existing is null)
        {
            existing = new MockApi { Id = Guid.NewGuid(), Code = dto.Code, Environment = environmentContext.Name, Name = dto.Name };
            dbContext.MockApis.Add(existing);
        }

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        // Environment is never client-supplied — it's always the environment this
        // portal instance is deployed as, not whatever the caller happened to send.
        existing.Environment = environmentContext.Name;
        existing.IsActive = dto.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        dbContext.MockMatchRules.RemoveRange(existing.Endpoints.SelectMany(e => e.Responses).SelectMany(r => r.MatchRules));
        dbContext.MockResponses.RemoveRange(existing.Endpoints.SelectMany(e => e.Responses));
        dbContext.MockEndpoints.RemoveRange(existing.Endpoints);

        existing.Endpoints = dto.Endpoints.Select(e => new MockEndpoint
        {
            Id = Guid.NewGuid(),
            MockApiId = existing.Id,
            Path = e.Path,
            HttpMethod = e.HttpMethod.ToUpperInvariant(),
            IsActive = e.IsActive,
            Responses = e.Responses.Select(r => new MockResponse
            {
                Id = Guid.NewGuid(),
                Name = r.Name,
                HttpStatusCode = r.HttpStatusCode,
                ResponseHeaders = r.ResponseHeaders,
                ResponseBody = r.ResponseBody,
                DelayMilliseconds = r.DelayMilliseconds,
                IsActive = r.IsActive,
                Priority = r.Priority,
                MatchRules = r.MatchRules.Select(m => new MockMatchRule
                {
                    Id = Guid.NewGuid(),
                    Source = m.Source,
                    Field = m.Field,
                    Operator = m.Operator,
                    ExpectedValue = m.ExpectedValue,
                }).ToList(),
            }).ToList(),
        }).ToList();

        await dbContext.SaveChangesAsync(ct);
        return ToDto(existing);
    }

    public async Task<Result<bool>> DeleteAsync(string code, CancellationToken ct)
    {
        var api = await dbContext.MockApis.SingleOrDefaultAsync(a => a.Code == code && a.Environment == environmentContext.Name, ct);
        if (api is null)
        {
            return Result<bool>.Failure(MockErrorCodes.MockApiNotFound, "Mock API was not found.");
        }

        dbContext.MockApis.Remove(api);
        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<MockApiDto>> SetActiveAsync(string code, bool isActive, CancellationToken ct)
    {
        var api = await FullTree().SingleOrDefaultAsync(a => a.Code == code, ct);
        if (api is null)
        {
            return Result<MockApiDto>.Failure(MockErrorCodes.MockApiNotFound, "Mock API was not found.");
        }

        api.IsActive = isActive;
        api.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);
        return Result<MockApiDto>.Success(ToDto(api));
    }

    private static MockApiDto ToDto(MockApi api) => new(
        api.Id, api.Code, api.Name, api.Description, api.Environment, api.IsActive,
        api.Endpoints.Select(e => new MockEndpointDto(
            e.Id, e.Path, e.HttpMethod, e.IsActive,
            e.Responses.Select(r => new MockResponseDto(
                r.Id, r.Name, r.HttpStatusCode, r.ResponseHeaders, r.ResponseBody, r.DelayMilliseconds, r.IsActive, r.Priority,
                r.MatchRules.Select(m => new MockMatchRuleDto(m.Id, m.Source, m.Field, m.Operator, m.ExpectedValue)).ToList()
            )).ToList()
        )).ToList());
}
