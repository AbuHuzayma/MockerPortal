using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Api.Common;
using Portal.Application.Audit;
using Portal.Application.Mocking;

namespace Portal.Api.Controllers;

/// <summary>
/// The generic /mock/{apiCode}/{**path} serving route — docs/09-api-mocker.md
/// §5. Called by other systems under test, not by an authenticated portal
/// user, so it is intentionally anonymous (network/environment placement is
/// the real access control) but every call is written to OperationLogs.
/// Excluded from Swagger: the catch-all route accepts every HTTP method (the
/// configured MockEndpoint decides which one matches), which OpenAPI cannot
/// describe as a single operation.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
[Route("mock/{apiCode}")]
public sealed class MockServingController(IMockEngine mockEngine, IOperationLogService operationLogService) : ControllerBase
{
    [Route("{**path}")]
    public async Task<IActionResult> Serve(string apiCode, string? path, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationIdAccessor.GetOrCreate(HttpContext);
        var commandName = $"MockServe:{apiCode}";

        string? body = null;
        if (Request.ContentLength is > 0)
        {
            using var reader = new StreamReader(Request.Body);
            body = await reader.ReadToEndAsync(ct);
        }

        var requestContext = new MockRequestContext(
            apiCode,
            path ?? string.Empty,
            Request.Method,
            Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase),
            Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase),
            body);

        var resolved = await mockEngine.ResolveAsync(requestContext, ct);

        if (resolved is null)
        {
            await LogAsync(correlationId, commandName, stopwatch, AuditResults.Failed, ct);
            return NotFound(new
            {
                code = MockErrorCodes.NoMockConfigured,
                message = $"No mock configured for {Request.Method} /{apiCode}/{path}",
                correlationId,
            });
        }

        if (resolved.DelayMilliseconds > 0)
        {
            await Task.Delay(resolved.DelayMilliseconds, ct);
        }

        foreach (var header in resolved.Headers)
        {
            Response.Headers[header.Key] = header.Value;
        }

        await LogAsync(correlationId, commandName, stopwatch, AuditResults.Success, ct);

        return new ContentResult
        {
            StatusCode = resolved.StatusCode,
            Content = resolved.Body ?? string.Empty,
            ContentType = resolved.Headers.GetValueOrDefault("Content-Type") ?? "application/json",
        };
    }

    private async Task LogAsync(string correlationId, string commandName, Stopwatch stopwatch, string result, CancellationToken ct) =>
        await operationLogService.WriteAsync(correlationId, commandName, stopwatch.ElapsedMilliseconds, result, ct);
}
