
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
}