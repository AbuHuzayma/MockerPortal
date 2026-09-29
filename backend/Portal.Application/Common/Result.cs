namespace Portal.Application.Common;

/// <summary>
/// Outcome of an operation that can fail in an expected way (invalid credentials,
/// not found, validation). Handlers return this instead of throwing for anticipated
/// failures — exceptions stay reserved for the unexpected (see docs/03-security.md §4).
/// </summary>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? code, string? message)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = code;
        ErrorMessage = message;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string code, string message) => new(false, default, code, message);
}
