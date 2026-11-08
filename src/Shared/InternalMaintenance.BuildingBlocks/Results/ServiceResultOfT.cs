namespace InternalMaintenance.BuildingBlocks.Results;

// Dùng khi cần trả Data
public sealed record ServiceResult<T>(
    bool IsSuccess,
    int StatusCode,
    string? Message,
    T? Data)
{
    public static ServiceResult<T> Success(
        T data,
        int statusCode = 200)
        => new(true, statusCode, null, data);

    public static ServiceResult<T> Fail(int statusCode, string message)
        => new(false, statusCode, message, default);
}
