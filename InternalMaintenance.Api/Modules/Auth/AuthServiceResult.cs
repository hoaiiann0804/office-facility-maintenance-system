using Microsoft.AspNetCore.Http;

namespace InternalMaintenance.Api.Modules.Auth;

public sealed record AuthServiceResult(
    bool IsSuccess,
    int StatusCode,
    string? Message)
{
    public static AuthServiceResult Success(int statusCode = StatusCodes.Status204NoContent)
        => new(true, statusCode, null);

    public static AuthServiceResult Fail(int statusCode, string message)
        => new(false, statusCode, message);
}

public sealed record AuthServiceResult<T>(
    bool IsSuccess,
    int StatusCode,
    string? Message,
    T? Data)
{
    public static AuthServiceResult<T> Success(
        T data,
        int statusCode = StatusCodes.Status200OK)
        => new(true, statusCode, null, data);

    public static AuthServiceResult<T> Fail(int statusCode, string message)
        => new(false, statusCode, message, default);
}
