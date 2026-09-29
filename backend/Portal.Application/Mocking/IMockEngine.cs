namespace Portal.Application.Mocking;

/// <summary>
/// Everything the request coming through the /mock/{apiCode}/{**path} route
/// (docs/09-api-mocker.md §5) needs to select a configured response.
/// </summary>
public sealed record MockRequestContext(
    string ApiCode,
    string Path,
    string HttpMethod,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string> QueryString,
    string? Body);

public sealed record MockResolvedResponse(int StatusCode, IReadOnlyDictionary<string, string> Headers, string? Body, int DelayMilliseconds);

public interface IMockEngine
{
    /// <returns>Null if no active MockApi/MockEndpoint matches, or no MockResponse's rules pass (and there's no ruleless default).</returns>
    Task<MockResolvedResponse?> ResolveAsync(MockRequestContext request, CancellationToken ct);
}
