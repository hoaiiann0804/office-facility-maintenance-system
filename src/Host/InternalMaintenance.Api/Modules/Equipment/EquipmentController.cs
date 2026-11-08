using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.Equipment;

[ApiController]
[Route("api/equipment")]
public sealed class EquipmentController(
    IEquipmentCatalog catalog,
    CurrentUserService currentUserService,
    IMaintenanceTicketLookup maintenanceTicketLookup) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<EquipmentResponse>>> GetEquipment(
        [FromQuery] EquipmentQuery query,
        CancellationToken cancellationToken)
    {
        var result = await catalog.GetPageAsync(
            query,
            currentUserService.Role == UserRoles.Admin,
            currentUserService.DepartmentId,
            cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentResponse>> GetEquipmentById(
        int id,
        CancellationToken cancellationToken)
    {
        var equipment = await catalog.GetByIdAsync(id, cancellationToken);
        if (equipment is null)
            return NotFound(new { message = "Equipment not found" });

        var role = currentUserService.Role;
        var departmentId = currentUserService.DepartmentId;
        if (role != UserRoles.Admin &&
            equipment.DepartmentId != departmentId &&
            equipment.MaintenanceDepartmentId != departmentId)
            return Forbid();

        return Ok(equipment);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<EquipmentResponse>> CreateEquipment(
        CreateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalog.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return CreatedAtAction(nameof(GetEquipmentById), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EquipmentResponse>> UpdateEquipment(
        int id,
        UpdateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        // Controller làm composition ở biên API: Maintenance trả lời ticket còn chiếm thiết bị
        // hay không; Assets nhận kết quả rồi tự cập nhật entity Equipment của mình.
        var hasOpenTicket = await maintenanceTicketLookup
            .HasEquipmentOccupyingTicketAsync(id, cancellationToken);
        var result = await catalog.UpdateAsync(id, request, hasOpenTicket, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEquipment(int id, CancellationToken cancellationToken)
    {
        var hasTicket = await maintenanceTicketLookup.HasAnyTicketForEquipmentAsync(id, cancellationToken);
        var result = await catalog.DeleteAsync(id, hasTicket, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return NoContent();
    }
}
