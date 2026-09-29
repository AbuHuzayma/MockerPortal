namespace Portal.Api.Common;

/// <summary>
/// Resolves (and if absent, generates) the correlation id for the current request.
/// Echoed on every response and logged, so a user-reported id is traceable end-to-end.
/// </summary>
public static class CorrelationIdAccessor
{
    public const string HeaderName = "X-Correlation-Id";

    public static string GetOrCreate(HttpContext context)
    {
        if (context.Items.TryGetValue(HeaderName, out var existing) && existing is string id)
        {
            return id;
        }

        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        return correlationId;
    }
}
