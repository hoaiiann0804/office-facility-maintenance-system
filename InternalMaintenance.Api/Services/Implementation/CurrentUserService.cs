using System.Security.Claims;

namespace InternalMaintenance.Api.Services;
public class CurrentUserService
{
    // Dùng để truy cập HttpContext của request hiện tại
    // HttpContext.User chứa thông tin user sau khi JWT được xác thực
    private readonly IHttpContextAccessor _httpContextAccessor;
    public CurrentUserService (IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    // Lấy ID của User đang gửi request.
    // Dùng cho CreatedByUserId, ChangedByUserId hoặc lọc dữ liệu theo user.
    public int UserId
    {
        get
        {
            var userIdValue = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);
            // Không có userId nghĩa là request chưa xác thực
            // Hoặc token thiếu claim cần thiết
            if(!int.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedAccessException(
                    "User id claim is missing or invalid"
                );
            } 
            return userId;
        }
    }
    // Lấy role của user từ JWT
    // Ví dụ : Admin, Manager, Staff, Technician 
    public string Role
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Role)
            ?? string.Empty;
        }
    }
    // Lấy email của user hiện tại 
    // Thường dùng cho Log, audit hoặc hiển thị thông tin
    public string Email
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        }
    }
    public int? DepartmentId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
            .FindFirstValue("departmentId");
           if(!int.TryParse(value, out var departmentId))
        {
            return null;
        }

        return departmentId;
        }
    }
    // Kiểm tra user có đang dùng temporary password hay không 
    // Nếu true thì chỉ nên  cho phép gọi API để đổi mật khẩu
    public bool MustChangePassword
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
            .FindFirstValue("mustChangePassword");

            return bool.TryParse(value, out var result) && result;
        }
    }

}
