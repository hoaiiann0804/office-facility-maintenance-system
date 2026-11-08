using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Dashboard;

/// <summary>
/// [VERTICAL SLICE: GET MAINTENANCE DASHBOARD STATISTICS - THỐNG KÊ TỔNG QUAN TRANG CHỦ]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/dashboard (DashboardController)
///           -> IMaintenanceDashboardReader.GetAsync(scope)
///           -> GetMaintenanceDashboardHandler.HandleAsync(scope)
///           -> Bước 1: Lọc `MaintenanceTickets` (`AsNoTracking`) theo phạm vi `TicketDashboardScope`
///              (CreatedByUserId, AssignedTechnicianId hoặc danh sách EquipmentIds của phòng ban).
///           -> Bước 2: Đếm số lượng Tổng cộng (`Total`), Đang mở (`Open`), Đã giải quyết (`Resolved`), Đã đóng (`Closed`)
///              và gom nhóm theo `Status` (`ByStatus`) &amp; `Priority` (`ByPriority`) ngay dưới SQL Server.
/// </summary>
public sealed class GetMaintenanceDashboardHandler(AppDbContext db)
{
    public async Task<MaintenanceDashboardStatistics> HandleAsync(
        TicketDashboardScope scope,
        CancellationToken cancellationToken = default)
    {
        var query = db.MaintenanceTickets.AsNoTracking().AsQueryable();

        if (scope.MatchCreatedOrAssigned)
        {
            query = query.Where(ticket =>
                ticket.CreatedByUserId == scope.CreatedByUserId ||
                ticket.AssignedTechnicianId == scope.AssignedTechnicianId);
        }
        else
        {
            if (scope.CreatedByUserId.HasValue)
                query = query.Where(ticket => ticket.CreatedByUserId == scope.CreatedByUserId.Value);
            if (scope.AssignedTechnicianId.HasValue)
                query = query.Where(ticket => ticket.AssignedTechnicianId == scope.AssignedTechnicianId.Value);
        }

        if (scope.RestrictToEquipmentIds)
        {
            var equipmentIds = scope.EquipmentIds ?? [];
            query = query.Where(ticket => equipmentIds.Contains(ticket.EquipmentId));
        }

        var total = await query.CountAsync(cancellationToken);
        var open = await query.CountAsync(ticket =>
            ticket.Status != "Closed" && ticket.Status != "Cancelled" && ticket.Status != "Resolved",
            cancellationToken);
        var resolved = await query.CountAsync(ticket => ticket.Status == "Resolved", cancellationToken);
        var closed = await query.CountAsync(ticket => ticket.Status == "Closed", cancellationToken);

        var byStatus = await query
            .GroupBy(ticket => ticket.Status)
            .Select(group => new DashboardCount(group.Key ?? "Unknown", group.Count()))
            .ToArrayAsync(cancellationToken);

        var byPriority = await query
            .GroupBy(ticket => ticket.Priority)
            .Select(group => new DashboardCount(group.Key ?? "Unknown", group.Count()))
            .ToArrayAsync(cancellationToken);

        return new MaintenanceDashboardStatistics(total, open, resolved, closed, byStatus, byPriority);
    }
}

