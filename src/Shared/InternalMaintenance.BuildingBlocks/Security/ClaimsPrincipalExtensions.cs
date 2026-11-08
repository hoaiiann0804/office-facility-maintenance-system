using System.Security.Claims;

namespace InternalMaintenance.BuildingBlocks.Security;

/// <summary>
/// [CLAIMS PRINCIPAL EXTENSIONS - TIỆN ÍCH ĐỌC CLAIMS CHO MINIMAL API ENDPOINTS]
///
/// 1. VẤN ĐỀ KIẾN TRÚC:
///    - Trong kiến trúc Vertical Slice theo phong cách FullStackHero, mỗi Slice (`Features/{FeatureName}/`)
///      sở hữu luôn file `{FeatureName}Endpoint.cs` (Minimal API).
///    - Nếu Endpoint nằm trong module `.Infrastructure` cần lấy `UserId`, `Role` hoặc `DepartmentId`
///      mà lại gọi `CurrentUserService` của `InternalMaintenance.Api`, module sẽ bị phụ thuộc ngược
///      vào Host (`InternalMaintenance.Api`), vi phạm `Architecture.Tests`.
///
/// 2. CÁCH GIẢI QUYẾT:
///    - Đặt các hàm mở rộng đọc trực tiếp từ `ClaimsPrincipal` (thuộc `System.Security.Claims` của .NET BCL)
///      ngay trong `InternalMaintenance.BuildingBlocks`.
///    - Nhờ đó, bất kỳ Minimal API Endpoint nào cũng có thể nhận `ClaimsPrincipal user` từ ASP.NET Core
///      và trích xuất thông tin người dùng hiện tại một cách độc lập, không cần tham chiếu ngược về Host!
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("User id claim is missing or invalid");
        }

        return userId;
    }

    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public static int? GetDepartmentId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("departmentId")?.Value;
        return int.TryParse(value, out var departmentId) ? departmentId : null;
    }
}

