namespace Portal.ApiTests;

internal sealed record ApiErrorBody(string Code, string Message);
internal sealed record ErrorEnvelope(bool Success, ApiErrorBody? Error);
internal sealed record DataEnvelope<T>(bool Success, T? Data);
