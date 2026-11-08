using InternalMaintenance.BuildingBlocks.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.Departments;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController(IDepartmentCatalog catalog) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<DepartmentResponse>>> GetDepartments(
        [FromQuery] DepartmentQuery query,
        CancellationToken cancellationToken)
    {
        var departments = await catalog.GetPageAsync(query, cancellationToken);
        return Ok(departments);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentResponse>> GetDepartmentById(
        int id,
        CancellationToken cancellationToken)
    {
        var department = await catalog.GetByIdAsync(id, cancellationToken);
        return department is null
            ? NotFound(new { message = "Department not found" })
            : Ok(department);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalog.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return CreatedAtAction(nameof(GetDepartmentById), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(
        int id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalog.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDepartment(int id, CancellationToken cancellationToken)
    {
        var result = await catalog.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return NoContent();
    }
}
