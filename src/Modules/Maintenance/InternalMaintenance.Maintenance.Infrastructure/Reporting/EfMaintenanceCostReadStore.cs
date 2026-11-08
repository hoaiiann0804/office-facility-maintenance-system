using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Reporting;

/// <summary>
/// Đọc log bảo trì và join Equipment/Department trong Persistence chung; mọi tổng hợp lớn chạy trên SQL.
/// </summary>
public sealed class EfMaintenanceCostReadStore(AppDbContext db) : IMaintenanceCostReadStore
{
    public async Task<MaintenanceCostReadResult> ReadAsync(
        MaintenanceCostReadQuery query,
        CancellationToken cancellationToken = default)
    {
        var logs = db.TicketVendorLogs.AsNoTracking().AsQueryable();
        if (query.IsAdministrator)
        {
            // Admin xem toàn bộ dữ liệu chi phí.
        }
        else if (query.ManagerDepartmentId.HasValue && query.CanViewReport)
        {
            logs = FilterByDepartment(logs, query.ManagerDepartmentId.Value);
        }
        else
        {
            logs = logs.Where(_ => false);
        }

        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value.Date;
            logs = logs.Where(log => log.DispatchedAt >= from);
        }
        if (query.ToDate.HasValue)
        {
            var untilExclusive = query.ToDate.Value.Date.AddDays(1);
            logs = logs.Where(log => log.DispatchedAt < untilExclusive);
        }
        if (query.DepartmentId.HasValue)
            logs = FilterByDepartment(logs, query.DepartmentId.Value);

        var totalCost = await logs.SumAsync(log => log.RepairCost ?? 0m, cancellationToken);
        var totalDispatches = await logs.CountAsync(cancellationToken);
        var distinctTickets = await logs.Select(log => log.MaintenanceTicketId)
            .Distinct()
            .CountAsync(cancellationToken);

        var monthStart = new DateTime(query.NowUtc.Year, query.NowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstMonth = monthStart.AddMonths(-5);
        var monthlyLogs = logs.Where(log => log.DispatchedAt >= firstMonth && log.DispatchedAt < monthStart.AddMonths(1));

        var monthlyCosts = await monthlyLogs
            .GroupBy(log => new { log.DispatchedAt.Year, log.DispatchedAt.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                TotalCost = group.Sum(log => log.RepairCost ?? 0m)
            })
            .ToListAsync(cancellationToken);
        var monthlyTicketCounts = await monthlyLogs
            .Select(log => new { log.DispatchedAt.Year, log.DispatchedAt.Month, log.MaintenanceTicketId })
            .Distinct()
            .GroupBy(item => new { item.Year, item.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, TicketCount = group.Count() })
            .ToListAsync(cancellationToken);
        var ticketCountByMonth = monthlyTicketCounts.ToDictionary(
            item => (item.Year, item.Month), item => item.TicketCount);
        var monthlyRows = monthlyCosts.Select(item => new MonthlyMaintenanceCostReadRow(
            item.Year,
            item.Month,
            item.TotalCost,
            ticketCountByMonth.GetValueOrDefault((item.Year, item.Month)))).ToArray();

        var vendorRows = await logs.GroupBy(log => log.VendorId)
            .Select(group => new VendorMaintenanceCostReadRow(
                group.Key,
                group.Sum(log => log.RepairCost ?? 0m),
                group.Count()))
            .ToListAsync(cancellationToken);

        var equipment = db.Equipment.AsNoTracking().AsQueryable();
        if (query.IsAdministrator)
        {
            // Admin xem thiết bị thuộc mọi phòng.
        }
        else if (query.ManagerDepartmentId.HasValue && query.CanViewReport)
        {
            var managerDepartmentId = query.ManagerDepartmentId.Value;
            equipment = equipment.Where(item =>
                item.DepartmentId == managerDepartmentId || item.MaintenanceDepartmentId == managerDepartmentId);
        }
        else
        {
            equipment = equipment.Where(_ => false);
        }

        var equipmentRows = await (
                from log in logs
                join ticket in db.MaintenanceTickets.AsNoTracking() on log.MaintenanceTicketId equals ticket.Id
                join asset in equipment on ticket.EquipmentId equals asset.Id
                join department in db.Departments.AsNoTracking()
                    on asset.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                group new { log, ticket, asset, department } by new
                {
                    asset.Id,
                    asset.Code,
                    asset.Name,
                    asset.DepartmentId,
                    DepartmentName = department == null ? null : department.Name
                } into groupByEquipment
                orderby groupByEquipment.Sum(row => row.log.RepairCost ?? 0m) descending
                select new EquipmentMaintenanceCostReadRow(
                    groupByEquipment.Key.Id,
                    groupByEquipment.Key.Code,
                    groupByEquipment.Key.Name,
                    groupByEquipment.Key.DepartmentId,
                    groupByEquipment.Key.DepartmentName,
                    groupByEquipment.Sum(row => row.log.RepairCost ?? 0m),
                    groupByEquipment.Select(row => row.ticket.Id).Distinct().Count()))
            .Take(5)
            .ToListAsync(cancellationToken);

        var managerDepartmentScope = query.ManagerDepartmentId ?? 0;
        var groupIntoManagerDepartment = query.ManagerDepartmentId.HasValue && !query.IsAdministrator;

        // Bước 1: GroupBy + Sum/Count thành một subquery đã tổng hợp xong (mỗi phòng ban 1 dòng).
        // LƯU Ý EF Core: KHÔNG được join tiếp lên `IGrouping` thô (group ... into g; join ... on g.Key)
        // vì SQL không có khái niệm "nhóm chưa tổng hợp" để JOIN -> lỗi "could not be translated".
        var departmentAggregates =
            from log in logs
            join ticket in db.MaintenanceTickets.AsNoTracking() on log.MaintenanceTicketId equals ticket.Id
            join asset in db.Equipment.AsNoTracking() on ticket.EquipmentId equals asset.Id
            group new { log.RepairCost, TicketId = ticket.Id } by groupIntoManagerDepartment
                ? managerDepartmentScope
                : (log.DepartmentIdAtDispatch ?? asset.DepartmentId) into groupByDepartment
            select new
            {
                DepartmentId = groupByDepartment.Key,
                TotalCost = groupByDepartment.Sum(row => row.RepairCost ?? 0m),
                TicketCount = groupByDepartment.Select(row => row.TicketId).Distinct().Count()
            };

        // Bước 2: LEFT JOIN kết quả đã tổng hợp với Departments để lấy tên phòng ban.
        var departmentRows = await (
                from aggregate in departmentAggregates
                join department in db.Departments.AsNoTracking()
                    on aggregate.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                select new DepartmentMaintenanceCostReadRow(
                    aggregate.DepartmentId,
                    department == null ? null : department.Name,
                    aggregate.TotalCost,
                    aggregate.TicketCount))
            .ToListAsync(cancellationToken);

        return new MaintenanceCostReadResult(
            totalCost,
            totalDispatches,
            distinctTickets,
            monthlyRows,
            vendorRows,
            equipmentRows,
            departmentRows);
    }

    private IQueryable<TicketVendorLog> FilterByDepartment(
        IQueryable<TicketVendorLog> logs,
        int departmentId) => logs.Where(log =>
        log.DepartmentIdAtDispatch == departmentId ||
        log.MaintenanceDepartmentIdAtDispatch == departmentId ||
        // Log cũ chưa có snapshot DepartmentId thì dùng phòng ban hiện tại của thiết bị làm phạm vi thay thế.
        (!log.DepartmentIdAtDispatch.HasValue && db.MaintenanceTickets.Any(ticket =>
            ticket.Id == log.MaintenanceTicketId && db.Equipment.Any(asset =>
                asset.Id == ticket.EquipmentId &&
                (asset.DepartmentId == departmentId || asset.MaintenanceDepartmentId == departmentId)))));
}
