using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Reporting;

/// <summary>
/// EF adapter của Maintenance: ticket, technician và phòng ban được tổng hợp chung trong SQL.
/// </summary>
public sealed class EfTechnicianPerformanceReadStore(AppDbContext db) : ITechnicianPerformanceReadStore
{
    public async Task<IReadOnlyCollection<TechnicianPerformanceReadRow>> ReadAsync(
        TechnicianPerformanceReadQuery query,
        CancellationToken cancellationToken = default)
    {
        var tickets = db.MaintenanceTickets.AsNoTracking().AsQueryable();

        // Phạm vi truy cập được áp vào SQL trước khi group để Manager không nhận số liệu phòng ban khác.
        if (query.IsAdministrator)
        {
            // Admin được xem toàn bộ ticket trong khoảng lọc.
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

        // GroupBy/Count/Average vẫn do SQL Server thực hiện; chỉ một dòng tổng hợp mỗi technician
        // đi qua mạng, tránh materialize toàn bộ lịch sử ticket trong API.
        var ticketAggregates = tickets
            .Where(ticket => ticket.AssignedTechnicianId.HasValue)
            .GroupBy(ticket => ticket.AssignedTechnicianId!.Value)
            .Select(group => new
            {
                TechnicianId = group.Key,
                TotalAssigned = group.Count(),
                InProgress = group.Count(ticket => ticket.Status == TicketStatuses.InProgress),
                WaitingForVendor = group.Count(ticket => ticket.Status == TicketStatuses.WaitingForVendor),
                Resolved = group.Count(ticket => ticket.Status == TicketStatuses.Resolved),
                Closed = group.Count(ticket => ticket.Status == TicketStatuses.Closed),
                // [FIX BÁO CÁO HIỆU SUẤT KTV - ĐỒNG BỘ SỐ LIỆU ĐẠT/TRỄ SLA VỚI PHIẾU ĐÃ XONG]:
                // 1. Các ticket mới khi Resolve đã chốt SlaStatus = MetSLA hoặc MissedSLA.
                // 2. Các ticket cũ trong DB (tạo/đóng trước migration AddSlaFields) có SlaStatus = "" hoặc "InSLA":
                //    đối chiếu ResolvedAt với DueAt (hoặc SlaStatus != Breached nếu không có DueAt) để mọi phiếu
                //    đã hoàn tất (Resolved + Closed) đều được phân loại vào Đạt SLA hoặc Trễ SLA, không bị rơi về 0.
                MetSla = group.Count(ticket =>
                    (ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Closed) &&
                    (ticket.SlaStatus == SlaPolicy.MetSLA ||
                     (ticket.SlaStatus != SlaPolicy.MissedSLA &&
                      ticket.SlaStatus != SlaPolicy.Breached &&
                      (!ticket.DueAt.HasValue ||
                       !ticket.ResolvedAt.HasValue ||
                       ticket.ResolvedAt.Value <= ticket.DueAt.Value)))),
                MissedSla = group.Count(ticket =>
                    (ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Closed) &&
                    (ticket.SlaStatus == SlaPolicy.MissedSLA ||
                     ticket.SlaStatus == SlaPolicy.Breached ||
                     (ticket.SlaStatus != SlaPolicy.MetSLA &&
                      ticket.DueAt.HasValue &&
                      ticket.ResolvedAt.HasValue &&
                      ticket.ResolvedAt.Value > ticket.DueAt.Value))),
                // [FIX MTTR]: Chỉ tính trên các ticket đã hoàn thành (Resolved/Closed) có ResolvedAt,
                // đồng thời trừ đi thời gian tạm dừng chờ đối tác (TotalSlaPausedMinutes * 60 giây).
                AverageResolutionSeconds = group
                    .Where(ticket =>
                        (ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Closed) &&
                        ticket.ResolvedAt.HasValue)
                    .Average(ticket => (double?)(
                        EF.Functions.DateDiffSecond(ticket.CreatedAt, ticket.ResolvedAt!.Value) > ticket.TotalSlaPausedMinutes * 60
                            ? EF.Functions.DateDiffSecond(ticket.CreatedAt, ticket.ResolvedAt!.Value) - (ticket.TotalSlaPausedMinutes * 60)
                            : 0))
            });

        var technicians =
            from user in db.Users.AsNoTracking()
            join role in db.Roles.AsNoTracking() on user.RoleId equals role.Id
            join department in db.Departments.AsNoTracking()
                on user.DepartmentId equals (int?)department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            where role.Name == UserRoles.Technician
            select new { User = user, DepartmentName = department == null ? null : department.Name };

        if (!query.IsAdministrator)
        {
            if (query.ManagerDepartmentId.HasValue && query.CanViewReport)
                technicians = technicians.Where(item => item.User.DepartmentId == query.ManagerDepartmentId.Value);
            else
                technicians = technicians.Where(_ => false);
        }

        if (query.DepartmentId.HasValue)
            technicians = technicians.Where(item => item.User.DepartmentId == query.DepartmentId.Value);
        if (query.TechnicianId.HasValue)
            technicians = technicians.Where(item => item.User.Id == query.TechnicianId.Value);

     
        var rows = await (
            from technician in technicians
            join aggregate in ticketAggregates
                on technician.User.Id equals aggregate.TechnicianId into aggregateGroup
            from aggregate in aggregateGroup.DefaultIfEmpty()
            select new
            {
                Id = technician.User.Id,
                FullName = technician.User.FullName,
                Email = technician.User.Email,
                DepartmentName = technician.DepartmentName,
                TotalAssigned = (int?)aggregate.TotalAssigned ?? 0,
                InProgress = (int?)aggregate.InProgress ?? 0,
                WaitingForVendor = (int?)aggregate.WaitingForVendor ?? 0,
                Resolved = (int?)aggregate.Resolved ?? 0,
                Closed = (int?)aggregate.Closed ?? 0,
                MetSla = (int?)aggregate.MetSla ?? 0,
                MissedSla = (int?)aggregate.MissedSla ?? 0,
                AverageResolutionSeconds = aggregate.AverageResolutionSeconds
            })
            .OrderByDescending(row => row.TotalAssigned)
            .Select(row => new TechnicianPerformanceReadRow(
                row.Id,
                row.FullName,
                row.Email,
                row.DepartmentName,
                row.TotalAssigned,
                row.InProgress,
                row.WaitingForVendor,
                row.Resolved,
                row.Closed,
                row.MetSla,
                row.MissedSla,
                row.AverageResolutionSeconds))
            .ToListAsync(cancellationToken);

        return rows;
    }

    private IQueryable<MaintenanceTicket> FilterByEquipmentDepartment(
        IQueryable<MaintenanceTicket> query,
        int departmentId) =>
        query.Where(ticket => db.Equipment.Any(equipment =>
            equipment.Id == ticket.EquipmentId &&
            (equipment.DepartmentId == departmentId || equipment.MaintenanceDepartmentId == departmentId)));
}
