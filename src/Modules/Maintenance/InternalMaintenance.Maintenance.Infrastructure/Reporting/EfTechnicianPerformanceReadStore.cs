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
                MetSla = group.Count(ticket =>
                    (ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Closed) &&
                    ticket.SlaStatus == SlaPolicy.MetSLA),
                MissedSla = group.Count(ticket =>
                    (ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Closed) &&
                    ticket.SlaStatus == SlaPolicy.MissedSLA),
                AverageResolutionSeconds = group
                    .Where(ticket => ticket.ResolvedAt.HasValue)
                    .Average(ticket => (double?)EF.Functions.DateDiffSecond(
                        ticket.CreatedAt, ticket.ResolvedAt!.Value))
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
