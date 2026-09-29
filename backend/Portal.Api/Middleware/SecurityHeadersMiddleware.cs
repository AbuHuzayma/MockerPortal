namespace Portal.Api.Middleware;

/// <summary>
/// Baseline OWASP-recommended response headers — docs/03-security.md §2.
/// This is a JSON API with no server-rendered HTML, so no CSP is set here;
/// the SPA (a separate origin) owns its own CSP if/when it adds one.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            return Task.CompletedTask;
        });

        return next(context);
    }
}
