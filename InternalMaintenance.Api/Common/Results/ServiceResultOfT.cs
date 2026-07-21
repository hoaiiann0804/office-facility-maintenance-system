namespace InternalMaintenance.Api.Common.Results;

// Dùng khi cần trả Data
public sealed record ServiceResult<T>(
    bool IsSuccess,
    int StatusCode,
    string? Message,
    T? Data)
{
    public static ServiceResult<T> Success(
        T data,
        int statusCode = StatusCodes.Status200OK)
        => new(true, statusCode, null, data);

    public static ServiceResult<T> Fail(int statusCode, string message)
        => new(false, statusCode, message, default);
}
