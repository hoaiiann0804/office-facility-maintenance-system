
using InternalMaintenance.Api.Common;
using InternalMaintenance.Api.DTOs.Common;
using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.DTOs.Users;
using InternalMaintenance.Api.Models;
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

        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            usersQuery = usersQuery.Where(
                user => user.FullName.Contains(keyword) ||
                user.Email.Contains(keyword)
            );
        }
        var role = query.Role?.Trim();
        if (!string.IsNullOrWhiteSpace(role))
        {
            usersQuery = usersQuery.Where(
                user => user.Role!.Name == role
            );
        }
        if (query.DepartmentId.HasValue)
        {
            usersQuery = usersQuery.Where(
                user => user.DepartmentId == query.DepartmentId
            );
        }

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

        usersQuery = usersQuery.ApplyPaging(query);

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
        return Ok(users.ToPagedResponse(query, totalItems));
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUserById(int id)
    {
        var user = await _context.Users
        .AsNoTracking()
        .Where(u => u.Id == id)
        .Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            RoleId = u.RoleId,
            RoleName = u.Role!.Name,
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department!.Name,
            IsActive = u.IsActive,
            MustChangePassword = u.MustChangePassword,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound(
                new
                {
                    message = "User not found"
                }
            );
        }

        return Ok(user);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLower();

        var emailExists = await _context.Users
        .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExists)
        {
            return BadRequest(
                new
                {
                    message = "User created exists "
                }
            );
        }

        var roleExists = await _context.Roles
        .AnyAsync(r => r.Id == request.RoleId);
        if (!roleExists)
        {
            return BadRequest(
                new
                {
                    message = "Role not found"
                });
        }
        var departmentExists = await _context.Departments
        .AnyAsync(d => d.Id == request.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(
                new
                {
                    message = "Department not found"
                });
        }
        var user = new User
        {
            FullName = fullName,
            Email = email,
            RoleId = request.RoleId,
            DepartmentId = request.DepartmentId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                request.TemporaryPassword
            ),
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var response = await _context.Users
        .Where(u => u.Id == user.Id)
        .Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            RoleId = u.RoleId,
            RoleName = u.Role!.Name,
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department!.Name,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).FirstOrDefaultAsync();

        return CreatedAtAction(
            nameof(GetUsers),
            new { id = user.Id },
            response

        );
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(int id, UpdateUserRequest request)
    {
        var fullName = request.FullName.Trim();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return NotFound(
                new
                {
                    message = "User not found"
                }
            );
        }

        // Chỉ cho phép Admin cập nhật thông tin nhân sự.
        // Email (đăng nhập) và Password (bảo mật) không được sửa tại đây.
        var roleExists = await _context.Roles
        .AnyAsync(r => r.Id == request.RoleId);
        if (!roleExists)
        {
            return BadRequest(
                new
                {
                    message = "Role not found"
                });
        }

        var departmentExists = await _context.Departments
        .AnyAsync(d => d.Id == request.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(
                new
                {
                    message = "Department not found"
                });
        }
        user.FullName = fullName;
        user.RoleId = request.RoleId;
        user.DepartmentId = request.DepartmentId;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var response = await _context.Users
        .Where(u => u.Id == user.Id)
        .Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            RoleId = u.RoleId,
            RoleName = u.Role!.Name,
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department!.Name,
            IsActive = u.IsActive,
            MustChangePassword = u.MustChangePassword,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).FirstOrDefaultAsync();
        return Ok(response);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<UserResponse>> UpdateUserStatus(int id, UpdateUserActiveRequest request)
    {
        var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return NotFound(
                new
                {
                    message = "User not found"
                }
            );
        }

        // Không cho phép Admin tự khóa tài khoản khi đăng nhập
        if (user.Id == _currentUserService.UserId &&
        !request.IsActive)
        {
            return BadRequest(
                new
                {
                    message = "You cannot deactivate your own account"
                }
            );
        }

        if (user.IsActive == request.IsActive)
        {
            return BadRequest(new
            {
                message = "User already has this status"
            });
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var response = await _context.Users
        .Where(u => u.Id == user.Id)
        .Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            RoleId = u.RoleId,
            RoleName = u.Role!.Name,
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department!.Name,
            IsActive = u.IsActive,
            MustChangePassword = u.MustChangePassword,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).FirstOrDefaultAsync();
        return Ok(response);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPatch("{id:int}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(int id, ResetUserPasswordRequest request)
    {
        var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return NotFound(
                new
                {
                    message = "User not found"
                }
            );
        }

        // Admin không tự reset cho mật khẩu chính mình
        if (user.Id == _currentUserService.UserId)
        {
            return BadRequest(
                new
                {
                    message = "Please use Change Password to update your own password"
                }
            );
        }

        // Reset mật khẩu tạm cho người dùng 
        // Sau lần đăng nhập tiếp theo, người dùng bắt buộc phải đổi mật khẩu mới

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.TemporaryPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(
             new
             {
                 message = "User password has been reset successfully."
             }
         );
    }
}
