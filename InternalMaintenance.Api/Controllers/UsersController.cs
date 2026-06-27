
using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.DTOs.Users;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUserService;

    public UsersController(AppDbContext context, CurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetUsers(
        [FromQuery] UserQuery query
    )
    {
        // Chỉ đọc dữ liệu nên không cần EF Core theo dõi thay đổi
        // Khởi tạo IQueryable để áp dụng filter, phân trang và sắp xếp trước khi thực thi SQL
        var usersQuery = _context.Users
        .AsNoTracking()
        .AsQueryable();

        // Tìm kiếm người dùng theo họ tên hoặc email 
        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            usersQuery = usersQuery.Where(
                user => user.FullName.Contains(keyword) ||
                user.Email.Contains(keyword)
            );
        }
        // Lọc danh sách người dùng theo vai trò 
        var role = query.Role?.Trim();
        if (!string.IsNullOrWhiteSpace(role))
        {
            usersQuery = usersQuery.Where(
                user => user.Role!.Name == role
            );
        }
        // Lọc người dùng thuộc phòng ban được chọn 
        if (query.DepartmentId.HasValue)
        {
            usersQuery = usersQuery.Where(
                user => user.DepartmentId == query.DepartmentId
            );
        }
        // Lọc tài khoản đang hoạt động hoặc đang bị vô hiệu hóa
        if (query.IsActive.HasValue)
        {
            usersQuery = usersQuery.Where(
                user => user.IsActive == query.IsActive
            );
        }
        // Đếm tổng số bản ghi sau khi áp dụng Filter
        // Phục vụ tính tổng số trang khi phân trang 
        var totalItems = await usersQuery.CountAsync();

        // Ưu tiên hiển thị Users mới tạo trước.
        // Nên cùng thời điểm tạo thì sắp Id để đảm bảo thứ tự ổn định khi phân trang
        usersQuery = usersQuery
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id);

        // Chỉ lấy dữ liệu của trang hiện tại 
        usersQuery = usersQuery.Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize);

        var users = await usersQuery.Select(
            user => new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role!.Name,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department!.Name,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            }
        ).ToListAsync();
        return Ok(new PagedResponse<UserResponse>
        {
            Items = users,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize)
        });
    }
}