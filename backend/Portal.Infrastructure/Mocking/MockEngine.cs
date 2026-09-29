using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Common;
using Portal.Application.Mocking;
using Portal.Infrastructure.Persistence;

namespace Portal.Infrastructure.Mocking;

/// <summary>
/// Matches an incoming /mock/{apiCode}/{**path} request against configured
/// MockResponses and applies {{token}} substitution — see
/// docs/09-api-mocker.md §3-4. This is closed-grammar string substitution
/// against a fixed whitelist of sources; it never evaluates an expression,
/// executes code, or runs SQL — see CLAUDE.md §12.
/// </summary>
public sealed partial class MockEngine(PortalDbContext dbContext, IEnvironmentContext environmentContext) : IMockEngine
{
    public async Task<MockResolvedResponse?> ResolveAsync(MockRequestContext request, CancellationToken ct)
    {
        var api = await dbContext.MockApis
            .Include(a => a.Endpoints).ThenInclude(e => e.Responses).ThenInclude(r => r.MatchRules)
            .SingleOrDefaultAsync(a => a.Code == request.ApiCode
                && a.IsActive
                && a.Environment == environmentContext.Name, ct);
        if (api is null)
        {
            return null;
        }

        var normalizedPath = request.Path.Trim('/');
        var endpoint = api.Endpoints.FirstOrDefault(e => e.IsActive
            && string.Equals(e.HttpMethod, request.HttpMethod, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.Path.Trim('/'), normalizedPath, StringComparison.Ordinal));
        if (endpoint is null)
        {
            return null;
        }

        var candidates = endpoint.Responses.Where(r => r.IsActive).OrderBy(r => r.Priority).ToList();

        // Ruled responses take precedence over the ruleless default, regardless
        // of priority order between the two groups (docs/09 §2).
        var selected = candidates.FirstOrDefault(r => r.MatchRules.Count > 0 && r.MatchRules.All(rule => EvaluateRule(rule, request)))
            ?? candidates.FirstOrDefault(r => r.MatchRules.Count == 0);

        if (selected is null)
        {
            return null;
        }

        var headers = string.IsNullOrWhiteSpace(selected.ResponseHeaders)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(selected.ResponseHeaders) ?? [];

        return new MockResolvedResponse(
            selected.HttpStatusCode,
            headers,
            ApplyTemplate(selected.ResponseBody, request),
            selected.DelayMilliseconds);
    }

    private static bool EvaluateRule(MockMatchRule rule, MockRequestContext request)
    {
        var actual = rule.Source switch
        {
            MockMatchSource.Header => request.Headers.GetValueOrDefault(rule.Field),
            MockMatchSource.QueryString => request.QueryString.GetValueOrDefault(rule.Field),
            MockMatchSource.Path => request.Path,
            MockMatchSource.RequestBody => ExtractFromBody(request.Body, rule.Field),
            _ => null,
        };

        if (actual is null)
        {
            return rule.Operator == MockMatchOperator.NotEquals;
        }

        return rule.Operator switch
        {
            MockMatchOperator.Equals => string.Equals(actual, rule.ExpectedValue, StringComparison.Ordinal),
            MockMatchOperator.NotEquals => !string.Equals(actual, rule.ExpectedValue, StringComparison.Ordinal),
            MockMatchOperator.Contains => actual.Contains(rule.ExpectedValue, StringComparison.Ordinal),
            MockMatchOperator.StartsWith => actual.StartsWith(rule.ExpectedValue, StringComparison.Ordinal),
            _ => false,
        };
    }

    /// <summary>Simple flat JSON property lookup, falling back to raw-body substring matching — never a full JSONPath/expression engine.</summary>
    private static string? ExtractFromBody(string? body, string field)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }

        if (string.IsNullOrEmpty(field) || field == ".")
        {
            return body;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(field, out var value))
            {
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
            }
        }
        catch (JsonException)
        {
            // Not JSON — fall through to raw-body substring matching below.
        }

        return body;
    }

    private static string ApplyTemplate(string? template, MockRequestContext request)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template ?? string.Empty;
        }

        return TokenPattern().Replace(template, match =>
        {
            var token = match.Groups[1].Value.Trim();
            if (token == "now")
            {
                return DateTime.UtcNow.ToString("O");
            }
            if (token == "uuid")
            {
                return Guid.NewGuid().ToString();
            }
            if (token.StartsWith("request.header.", StringComparison.Ordinal))
            {
                return request.Headers.GetValueOrDefault(token["request.header.".Length..]) ?? string.Empty;
            }
            if (token.StartsWith("request.query.", StringComparison.Ordinal))
            {
                return request.QueryString.GetValueOrDefault(token["request.query.".Length..]) ?? string.Empty;
            }
            if (token.StartsWith("request.body.", StringComparison.Ordinal))
            {
                return ExtractFromBody(request.Body, token["request.body.".Length..]) ?? string.Empty;
            }

            return match.Value; // unknown token — left as-is rather than guessed at
        });
    }

    [GeneratedRegex(@"\{\{([^{}]+)\}\}")]
    private static partial Regex TokenPattern();
}
