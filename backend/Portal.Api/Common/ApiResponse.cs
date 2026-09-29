namespace Portal.Api.Common;

/// <summary>
/// Standard success/error envelope for every API response. See docs/06-api-design.md.
/// </summary>
public sealed class ApiResponse<T>
{
    public required bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public required string CorrelationId { get; init; }

    public static ApiResponse<T> Ok(T data, string correlationId) => new()
    {
        Success = true,
        Data = data,
        CorrelationId = correlationId
    };

    public static ApiResponse<T> Fail(ApiError error, string correlationId) => new()
    {
        Success = false,
        Error = error,
        CorrelationId = correlationId
    };
}

public sealed class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public object? Details { get; init; }
}
