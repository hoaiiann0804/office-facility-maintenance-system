
using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.DTOs.Common;
using InternalMaintenance.Api.DTOs.Departments;
using InternalMaintenance.Api.DTOs.MaintenanceTicket;
using InternalMaintenance.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace InternalMaintenance.Api.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<DepartmentResponse>>> GetDepartments(
        [FromQuery] DepartmentQuery query
    )
    {
        var departmentQuery = _context.Departments
        .AsNoTracking()
        .AsQueryable();

        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            departmentQuery = departmentQuery.Where(
                department => department.Name.Contains(keyword)
            );
        }

        var totalItems = await departmentQuery.CountAsync();

        departmentQuery = departmentQuery
        .OrderByDescending(department => department.CreatedAt)
        .ThenBy(department => department.Id);

        departmentQuery = departmentQuery.Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize);


        var departments = await departmentQuery
        .Select(department => new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            CreatedAt = department.CreatedAt

        }).ToListAsync();
        return Ok(
            new PagedResponse<DepartmentResponse>
            {
                Items = departments,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize)
            }
        );
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentResponse>> GetDepartmentById(int id)
    {
        //Where = lọc dữ liệu
        // Select = chọn dữ liệu muốn trả về
        // FirstOrDefaultAsync = lấy 1 dòng đầu tiên hoặc null
        var department = await _context.Departments
        .Where(department => department.Id == id)
        .Select(department => new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            CreatedAt = department.CreatedAt
        })
        .FirstOrDefaultAsync();
        if (department is null)
        {
            return NotFound(new
            {
                message = "Department not found"
            });
        }
        return Ok(department);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment(CreateDepartmentRequest request)
    {
        // Kiểm tra Tên phòng ban có tồn tại hay không 
        var normalizedName = request.Name.Trim();

        var isDuplicate = await _context.Departments.AnyAsync(d => d.Name == normalizedName);

        if (isDuplicate)
        {
            return BadRequest(new
            {
                message = "Department name already exists"
            });
        }

        var department = new Department
        {
            Name = normalizedName,
            Description = request.Description?.Trim()
        };
        _context.Departments.Add(department);
        //Lưu thay đổi xuống Database
        // Sau khi lưu , database sẽ sinh Id cho department
        await _context.SaveChangesAsync();

        var response = new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            CreatedAt = department.CreatedAt
        };
        //Sau khi tạo xong department, Api sẽ chỉ client biết có thể lấy department vừa tạo bằng action GetDepartmentById.
        return CreatedAtAction(
            nameof(GetDepartmentById),
            new { id = department.Id },
            response
        );
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(int id, UpdateDepartmentRequest request)
    {
        // 1. Lấy department có Id = 2 trong database
        // 2. Nếu không có → trả 404 Not Found
        // 3. Nếu có → sửa Name, Description
        // 4. SaveChangesAsync()
        // 5. Trả về department sau khi update
        var department = await _context.Departments
        .FirstOrDefaultAsync(d => d.Id == id);

        if (department is null)
        {
            return NotFound(
                new
                {
                    message = "Department not found"
                }
            );
        }

        // Kiểm tra Tên phòng ban có tồn tại hay không 
        var normalizedName = request.Name.Trim();
        var isDuplicate = await _context.Departments.AnyAsync(d => d.Name == normalizedName);
        if (isDuplicate)
        {
            return BadRequest(
                new
                {
                    message = "Department name already exists"
                }
            );
        }
        department.Name = normalizedName;
        department.Description = request.Description?.Trim();
        await _context.SaveChangesAsync();

        var response = new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            CreatedAt = department.CreatedAt
        };

        return Ok(response);
    }
    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Department>> DeletePartment(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id);

        if (department is null)
        {
            return NotFound(
                new
                {
                    message = "Department not found"
                }
            );
        }

        //Kiểm tra phòng ban nào có User đang thuộc về hay không 
        var hasUsers = await _context.Users.AnyAsync(user => user.DepartmentId == id);
        //Kiểm tra phòng ban nào có Thiết bị nào đang thuộc về không
        var hasEquipment = await _context.Equipment.AnyAsync(equipment => equipment.DepartmentId == id);

        // Nếu còn dữ liệu liên quan thì không cho xóa 
        if (hasUsers || hasEquipment)
        {
            return BadRequest(
                new
                {
                    message = "Cannot delete department because it has related users or equipment"
                }
            );
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return NoContent();

    }
}