using InternalMaintenance.Api.Modules.Dashboard.Contracts;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.Dashboard;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(
    IMaintenanceDashboardReader maintenanceReader,
    IEquipmentDashboardReader equipmentReader,
    IOrganizationDirectory organizationDirectory,
    IIdentityUserDirectory identityDirectory,
    CurrentUserService currentUserService) : ControllerBase
{
    private async Task<bool> IsMaintenanceManagerAsync(CancellationToken cancellationToken) =>
        currentUserService.Role == UserRoles.Manager &&
        currentUserService.DepartmentId.HasValue &&
        await organizationDirectory.IsMaintenanceTeamAsync(
            currentUserService.DepartmentId.Value, cancellationToken);

    private async Task<TicketDashboardScope> GetTicketScopeAsync(CancellationToken cancellationToken)
    {
        var role = currentUserService.Role;
        var userId = currentUserService.UserId;

        if (role == UserRoles.Technician)
            return new TicketDashboardScope(
                CreatedByUserId: userId,
                AssignedTechnicianId: userId,
                MatchCreatedOrAssigned: true);

        if (role == UserRoles.Staff)
            return new TicketDashboardScope(CreatedByUserId: userId);

        if (role == UserRoles.Manager && !await IsMaintenanceManagerAsync(cancellationToken))
        {
            // Dashboard của Manager giới hạn theo phòng ban sở hữu thiết bị.
            // Assets trả về ID để Maintenance áp dụng filter trên ticket mà không cần entity join ở controller.
            var equipmentIds = await equipmentReader.FindEquipmentIdsByDepartmentAsync(
                currentUserService.DepartmentId, cancellationToken);
            return new TicketDashboardScope(
                EquipmentIds: equipmentIds,
                RestrictToEquipmentIds: true);
        }

        return new TicketDashboardScope();
    }

    private async Task<bool> RestrictEquipmentToDepartmentAsync(CancellationToken cancellationToken) =>
        currentUserService.Role == UserRoles.Staff ||
        (currentUserService.Role == UserRoles.Manager &&
         !await IsMaintenanceManagerAsync(cancellationToken));

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var ticketScope = await GetTicketScopeAsync(cancellationToken);
        var equipmentRestricted = await RestrictEquipmentToDepartmentAsync(cancellationToken);
        var tickets = await maintenanceReader.GetAsync(ticketScope, cancellationToken);
        var equipment = await equipmentReader.GetAsync(
            currentUserService.DepartmentId, equipmentRestricted, cancellationToken);
        var technicianCount = await identityDirectory.CountUsersInRoleAsync(
            UserRoles.Technician, cancellationToken);
        var departmentCount = await organizationDirectory.CountAsync(cancellationToken);

        return Ok(new DashboardSummaryResponse
        {
            TotalTickets = tickets.TotalTickets,
            OpenTickets = tickets.OpenTickets,
            ResolvedTickets = tickets.ResolvedTickets,
            ClosedTickets = tickets.ClosedTickets,
            TotalEquipment = equipment.TotalEquipment,
            ActiveEquipment = equipment.ActiveEquipment,
            UnderMaintenanceEquipment = equipment.UnderMaintenanceEquipment,
            TotalTechnicians = technicianCount,
            TotalDepartments = departmentCount
        });
    }

    [HttpGet("charts")]
    public async Task<ActionResult<ChartDataResponse>> GetCharts(CancellationToken cancellationToken)
    {
        var ticketScope = await GetTicketScopeAsync(cancellationToken);
        var equipmentRestricted = await RestrictEquipmentToDepartmentAsync(cancellationToken);
        var tickets = await maintenanceReader.GetAsync(ticketScope, cancellationToken);
        var equipment = await equipmentReader.GetAsync(
            currentUserService.DepartmentId, equipmentRestricted, cancellationToken);
        var departmentIds = equipment.EquipmentByDepartment
            .Select(item => item.DepartmentId)
            .Distinct()
            .ToArray();
        var departmentNames = await organizationDirectory.FindNamesAsync(departmentIds, cancellationToken);

        return Ok(new ChartDataResponse
        {
            TicketsByStatus = tickets.TicketsByStatus
                .Select(item => new ChartItem { Name = item.Name, Value = item.Value })
                .ToList(),
            TicketsByPriority = tickets.TicketsByPriority
                .Select(item => new ChartItem { Name = item.Name, Value = item.Value })
                .ToList(),
            EquipmentByDepartment = equipment.EquipmentByDepartment
                .Select(item => new ChartItem
                {
                    Name = departmentNames.GetValueOrDefault(item.DepartmentId, "No Department"),
                    Value = item.Count
                })
                .ToList()
        });
    }
}
