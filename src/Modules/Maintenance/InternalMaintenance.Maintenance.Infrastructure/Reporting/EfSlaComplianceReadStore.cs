using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Reporting;

/// <summary>EF adapter cho báo cáo SLA; quyền, lọc, group và đếm được thực thi trên SQL Server.</summary>
public sealed class EfSlaComplianceReadStore(AppDbContext db) : ISlaComplianceReadStore
{
    public async Task<SlaComplianceReadResult> ReadAsync(
        SlaComplianceReadQuery query,
        CancellationToken cancellationToken = default)
    {
        var tickets = db.MaintenanceTickets.AsNoTracking().AsQueryable();

        // Manager chỉ đọc ticket của thiết bị trong phòng mình; người không có quyền nhận tập rỗng.
        if (query.IsAdministrator)
        {
            // Admin được xem mọi phòng ban.
        }
        else if (query.ManagerDepartmentId.HasValue && query.CanViewReport)
        {
            tickets = FilterByEquipmentDepartment(tickets, query.ManagerDepartmentId.Value);
        }
        else
        {
            tickets = tickets.Where(_ => false);
        }

        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value.Date;
            tickets = tickets.Where(ticket => ticket.CreatedAt >= from);
        }

        if (query.ToDate.HasValue)
        {
            var untilExclusive = query.ToDate.Value.Date.AddDays(1);
            tickets = tickets.Where(ticket => ticket.CreatedAt < untilExclusive);
        }

        if (query.DepartmentId.HasValue)
            tickets = FilterByEquipmentDepartment(tickets, query.DepartmentId.Value);
        if (query.TechnicianId.HasValue)
            tickets = tickets.Where(ticket => ticket.AssignedTechnicianId == query.TechnicianId.Value);

        var totalTickets = await tickets.CountAsync(cancellationToken);

        // WaitingForVendor vẫn xuất hiện trong báo cáo đang hoạt động dù SLA của nó đang tạm dừng.
        var activeStatuses = TicketWorkflowRules.SlaTrackedStatuses
            .Append(TicketStatuses.WaitingForVendor)
            .ToArray();
        var activeCounts = await tickets
            .Where(ticket => activeStatuses.Contains(ticket.Status))
            .GroupBy(ticket => ticket.SlaStatus)
            .Select(group => new { SlaStatus = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.SlaStatus, item => item.Count, cancellationToken);

        // Chỉ Resolved/Closed có kết quả Met/Missed cuối cùng mới tham gia mẫu số SLA.
        var completed = tickets.Where(ticket =>
            (ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Closed) &&
            (ticket.SlaStatus == SlaPolicy.MetSLA || ticket.SlaStatus == SlaPolicy.MissedSLA));

        var completedCounts = await completed
            .GroupBy(ticket => ticket.SlaStatus)
            .Select(group => new { SlaStatus = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.SlaStatus, item => item.Count, cancellationToken);

        var groupedPriorities = await completed
            .GroupBy(ticket => ticket.Priority)
            .Select(group => new SlaPriorityReadRow(
                group.Key,
                group.Count(),
                group.Count(ticket => ticket.SlaStatus == SlaPolicy.MetSLA),
                group.Count(ticket => ticket.SlaStatus == SlaPolicy.MissedSLA)))
            .ToListAsync(cancellationToken);
        var prioritiesByName = groupedPriorities.ToDictionary(item => item.Priority);
        // Trả đủ bốn priority để Reporting luôn giữ response ổn định kể cả nhóm nào chưa có ticket.
        var priorityRows = new[]
        {
            TicketPriorities.Critical,
            TicketPriorities.High,
            TicketPriorities.Medium,
            TicketPriorities.Low
        }.Select(priority => prioritiesByName.GetValueOrDefault(priority) ??
            new SlaPriorityReadRow(priority, 0, 0, 0)).ToArray();

        var firstMonth = new DateTime(query.NowUtc.Year, query.NowUtc.Month, 1).AddMonths(-5);
        var nextMonth = new DateTime(query.NowUtc.Year, query.NowUtc.Month, 1).AddMonths(1);
        var monthlyRows = await completed
            .Where(ticket => ticket.CreatedAt >= firstMonth && ticket.CreatedAt < nextMonth)
            .GroupBy(ticket => new { ticket.CreatedAt.Year, ticket.CreatedAt.Month })
            .Select(group => new SlaMonthlyReadRow(
                group.Key.Year,
                group.Key.Month,
                group.Count(ticket => ticket.SlaStatus == SlaPolicy.MetSLA),
                group.Count(ticket => ticket.SlaStatus == SlaPolicy.MissedSLA)))
            .ToListAsync(cancellationToken);

        return new SlaComplianceReadResult(
            totalTickets,
            activeCounts.GetValueOrDefault(SlaPolicy.InSLA),
            activeCounts.GetValueOrDefault(SlaPolicy.NearBreach),
            activeCounts.GetValueOrDefault(SlaPolicy.Breached),
            activeCounts.GetValueOrDefault(SlaPolicy.Paused),
            completedCounts.GetValueOrDefault(SlaPolicy.MetSLA),
            completedCounts.GetValueOrDefault(SlaPolicy.MissedSLA),
            priorityRows,
            monthlyRows);
    }

    private IQueryable<MaintenanceTicket> FilterByEquipmentDepartment(
        IQueryable<MaintenanceTicket> query,
        int departmentId) =>
        query.Where(ticket => db.Equipment.Any(equipment =>
            equipment.Id == ticket.EquipmentId &&
            (equipment.DepartmentId == departmentId || equipment.MaintenanceDepartmentId == departmentId)));
}
