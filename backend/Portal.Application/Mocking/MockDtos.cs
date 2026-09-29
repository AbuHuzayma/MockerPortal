namespace Portal.Application.Mocking;

public sealed record MockMatchRuleDto(Guid? Id, string Source, string Field, string Operator, string ExpectedValue);

public sealed record MockResponseDto(
    Guid? Id,
    string Name,
    int HttpStatusCode,
    string? ResponseHeaders,
    string? ResponseBody,
    int DelayMilliseconds,
    bool IsActive,
    int Priority,
    List<MockMatchRuleDto> MatchRules);

public sealed record MockEndpointDto(Guid? Id, string Path, string HttpMethod, bool IsActive, List<MockResponseDto> Responses);

public sealed record MockApiDto(
    Guid? Id,
    string Code,
    string Name,
    string? Description,
    string Environment,
    bool IsActive,
    List<MockEndpointDto> Endpoints);

public static class MockErrorCodes
{
    public const string MockApiNotFound = "MOCK_API_NOT_FOUND";
    public const string NoMockConfigured = "NO_MOCK_CONFIGURED";
}
