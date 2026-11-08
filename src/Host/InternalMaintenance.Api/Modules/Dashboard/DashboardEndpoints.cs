using InternalMaintenance.Api.Modules.Dashboard.Contracts;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Api.Modules.Dashboard;

/// <summary>
/// [MINIMAL API ENDPOINTS - DASHBOARD COMPOSITION]
///
/// - Luồng đi (Request Flow):
///   1. HTTP GET /api/dashboard/summary
///      -> Tổng hợp số liệu từ IMaintenanceDashboardReader, IEquipmentDashboardReader,
///         IIdentityUserDirectory, IOrganizationDirectory.
///   2. HTTP GET /api/dashboard/charts
///      -> Tổng hợp biểu đồ trạng thái/độ ưu tiên ticket và thiết bị theo phòng ban.
/// </summary>
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("summary", async (
            IMaintenanceDashboardReader maintenanceReader,
            IEquipmentDashboardReader equipmentReader,
            IOrganizationDirectory organizationDirectory,
            IIdentityUserDirectory identityDirectory,
            CurrentUserService currentUserService,
            CancellationToken cancellationToken) =>
        {
            var ticketScope = await GetTicketScopeAsync(
                currentUserService,
                organizationDirectory,
                equipmentReader,
                cancellationToken);
            var equipmentRestricted = await RestrictEquipmentToDepartmentAsync(
                currentUserService,
                organizationDirectory,
                cancellationToken);
            var tickets = await maintenanceReader.GetAsync(ticketScope, cancellationToken);
            var equipment = await equipmentReader.GetAsync(
                currentUserService.DepartmentId, equipmentRestricted, cancellationToken);
            var technicianCount = await identityDirectory.CountUsersInRoleAsync(
                UserRoles.Technician, cancellationToken);
            var departmentCount = await organizationDirectory.CountAsync(cancellationToken);

            return Results.Ok(new DashboardSummaryResponse
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
        })
        .WithName("GetDashboardSummary")
        .Produces<DashboardSummaryResponse>(StatusCodes.Status200OK);

        group.MapGet("charts", async (
            IMaintenanceDashboardReader maintenanceReader,
            IEquipmentDashboardReader equipmentReader,
            IOrganizationDirectory organizationDirectory,
            CurrentUserService currentUserService,
            CancellationToken cancellationToken) =>
        {
            var ticketScope = await GetTicketScopeAsync(
                currentUserService,
                organizationDirectory,
                equipmentReader,
                cancellationToken);
            var equipmentRestricted = await RestrictEquipmentToDepartmentAsync(
                currentUserService,
                organizationDirectory,
                cancellationToken);
            var tickets = await maintenanceReader.GetAsync(ticketScope, cancellationToken);
            var equipment = await equipmentReader.GetAsync(
                currentUserService.DepartmentId, equipmentRestricted, cancellationToken);
            var departmentIds = equipment.EquipmentByDepartment
                .Select(item => item.DepartmentId)
                .Distinct()
                .ToArray();
            var departmentNames = await organizationDirectory.FindNamesAsync(departmentIds, cancellationToken);

            return Results.Ok(new ChartDataResponse
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
        })
        .WithName("GetDashboardCharts")
        .Produces<ChartDataResponse>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<bool> IsMaintenanceManagerAsync(
        CurrentUserService currentUserService,
        IOrganizationDirectory organizationDirectory,
        CancellationToken cancellationToken) =>
        currentUserService.Role == UserRoles.Manager &&
        currentUserService.DepartmentId.HasValue &&
        await organizationDirectory.IsMaintenanceTeamAsync(
            currentUserService.DepartmentId.Value, cancellationToken);

    private static async Task<TicketDashboardScope> GetTicketScopeAsync(
        CurrentUserService currentUserService,
        IOrganizationDirectory organizationDirectory,
        IEquipmentDashboardReader equipmentReader,
        CancellationToken cancellationToken)
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

        if (role == UserRoles.Manager &&
            !await IsMaintenanceManagerAsync(currentUserService, organizationDirectory, cancellationToken))
        {
            var equipmentIds = await equipmentReader.FindEquipmentIdsByDepartmentAsync(
                currentUserService.DepartmentId, cancellationToken);
            return new TicketDashboardScope(
                EquipmentIds: equipmentIds,
                RestrictToEquipmentIds: true);
        }

        return new TicketDashboardScope();
    }

    private static async Task<bool> RestrictEquipmentToDepartmentAsync(
        CurrentUserService currentUserService,
        IOrganizationDirectory organizationDirectory,
        CancellationToken cancellationToken) =>
        currentUserService.Role == UserRoles.Staff ||
        (currentUserService.Role == UserRoles.Manager &&
         !await IsMaintenanceManagerAsync(currentUserService, organizationDirectory, cancellationToken));
}

