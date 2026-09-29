using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Portal.Api.Common;

/// <summary>
/// Writes health check results in the standard API envelope shape instead of the
/// default plain-text "Healthy"/"Unhealthy" body.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task Write(HttpContext context, HealthReport report)
    {
        var correlationId = CorrelationIdAccessor.GetOrCreate(context);

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };

        var response = ApiResponse<object>.Ok(payload, correlationId);

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
