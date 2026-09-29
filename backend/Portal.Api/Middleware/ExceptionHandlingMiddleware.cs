using System.Net;
using System.Text.Json;
using Portal.Api.Common;

namespace Portal.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and converts them into the standard error envelope.
/// Full exception detail is logged server-side only — never returned to the client.
/// See docs/03-security.md §4.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationIdAccessor.GetOrCreate(context);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

            var response = ApiResponse<object>.Fail(
                new ApiError
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An unexpected error occurred. Please retry or contact support with the correlation id."
                },
                correlationId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
