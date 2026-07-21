
namespace InternalMaintenance.Api.Common.Results;

// Dùng khi không cần trả data
public sealed record ServiceResult(
    // IsSucess: action có hành động hay không 
    // StatusCode: HTTP status code nên trả về. 
    // Message: Thông báo lỗi hoặc có thể để null khi thành công
    bool IsSuccess,
    int StatusCode,
    string? Message)
{
    // Dùng để tạo kết quả thành công 
    public static ServiceResult Success(int statusCode = StatusCodes.Status204NoContent)
        => new(true, statusCode, null);
    // Tạo kết quả thất bại
    public static ServiceResult Fail(int statusCode, string message)
        => new(false, statusCode, message);
}
