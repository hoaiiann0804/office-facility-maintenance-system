using InternalMaintenance.Persistence;
using InternalMaintenance.Assets.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Assets.Infrastructure;

/// <summary>Assets thực thi các phép tổng hợp trên thiết bị thay vì để Dashboard truy cập DbSet của module.</summary>
public sealed class EquipmentDashboardReader(AppDbContext db) : IEquipmentDashboardReader
{
    public async Task<IReadOnlyCollection<int>> FindEquipmentIdsByDepartmentAsync(
        int? departmentId,
        CancellationToken cancellationToken = default)
    {
        // Phạm vi Dashboard của Manager theo DepartmentId sở hữu thiết bị, không theo MaintenanceDepartmentId.
        return await db.Equipment
            .AsNoTracking()
            .Where(equipment => equipment.DepartmentId == departmentId)
            .Select(equipment => equipment.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<EquipmentDashboardStatistics> GetAsync(
        int? departmentId,
        bool restrictToDepartment,
        CancellationToken cancellationToken = default)
    {
        var query = db.Equipment.AsNoTracking().AsQueryable();
        if (restrictToDepartment)
        {
            query = departmentId.HasValue
                ? query.Where(equipment => equipment.DepartmentId == departmentId.Value)
                : query.Where(_ => false);
        }

        var total = await query.CountAsync(cancellationToken);
        var active = await query.CountAsync(equipment => equipment.Status == "Active", cancellationToken);
        var underMaintenance = await query.CountAsync(
            equipment => equipment.Status == "UnderMaintenance", cancellationToken);
        var byDepartment = await query
            .GroupBy(equipment => equipment.DepartmentId)
            .Select(group => new EquipmentDepartmentCount(group.Key, group.Count()))
            .ToArrayAsync(cancellationToken);

        return new EquipmentDashboardStatistics(total, active, underMaintenance, byDepartment);
    }
}
