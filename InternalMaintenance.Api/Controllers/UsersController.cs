
using InternalMaintenance.Api.DTOs.Common;
using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.DTOs.Users;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalMaintenance.Api.Models;

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

}