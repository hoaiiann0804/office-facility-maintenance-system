
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
namespace InternalMaintenance.Api.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController: ControllerBase
{
    private readonly AppDbContext _context;
    public DepartmentsController (AppDbContext context)
    {
        _context = context;
    }
    [HttpGet]
    public async Task<ActionResult<List<Department>>> GetDepartments()
    {
        var departments  = await _context.Departments.ToListAsync();
        return Ok(departments);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<List<Department>>> GetDepartmentById(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(department => department.Id == id);
        if(department == null)
        {
            return NotFound(new
            {
                 message = "Department not found"
            });
        }
        return Ok(department);
    }
    
    [HttpPost]
    public async Task<ActionResult<Department>> CreateDepartment(Department department){
        _context.Departments.Add(department);
        //Lưu thay đổi xuống Database
        // Sau khi lưu , database sẽ sinh Id cho department
        await _context.SaveChangesAsync();
       
       //Sau khi tạo xong department, Api sẽ chỉ client biết có thể lấy department vừa tạo bằng action GetDepartmentById.
        return CreatedAtAction(
            nameof(GetDepartmentById),
            new {id = department.Id},
            department
        );
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Department>> UpdateDepartment( int id, Department department)
    {
        // 1. Lấy department có Id = 2 trong database
        // 2. Nếu không có → trả 404 Not Found
        // 3. Nếu có → sửa Name, Description
        // 4. SaveChangesAsync()
        // 5. Trả về department sau khi update
        var existingDepartment  = await _context.Departments.FirstOrDefaultAsync(d => d.Id==id);
        
        if(existingDepartment is null)
        {
            return NotFound(
                new
                {
                    message = "Department not found"
                }
            );
        }
        existingDepartment.Name = department.Name;
        existingDepartment.Description = department.Description;
        await _context.SaveChangesAsync();
        
        return Ok(existingDepartment);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Department>> DeletePartment(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d=>d.Id==id);
        
        if(department is null)
        {
            return NotFound(
                new
                {
                    message = "Department not found"
                }
            );
        }
        //Kiểm tra phòng ban nào có User đang thuộc về hay không 
        var hasUsers = await _context.Users.AnyAsync(user=>user.DepartmentId ==id);
        //Kiểm tra phòng ban nào có Thiết bị nào đang thuộc về không
        var hasEquipment = await _context.Equipment.AnyAsync(equipment=>equipment.DepartmentId == id);
        
        // Nếu còn dữ liệu liên quan thì không cho xóa 
        if(hasUsers || hasEquipment)
        {
            return BadRequest(
                new
                {
                    message="Cannot delete department because it has related users or equipment"
                }
            );
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return NoContent();

    }
}