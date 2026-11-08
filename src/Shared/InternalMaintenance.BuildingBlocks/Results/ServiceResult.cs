
namespace InternalMaintenance.BuildingBlocks.Results;

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
    // 204 = HTTP No Content; giữ numeric status để BuildingBlocks không cần tham chiếu ASP.NET Core.
    public static ServiceResult Success(int statusCode = 204)
        => new(true, statusCode, null);
    // Tạo kết quả thất bại
    public static ServiceResult Fail(int statusCode, string message)
        => new(false, statusCode, message);
}
