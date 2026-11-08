using InternalMaintenance.Persistence;
using InternalMaintenance.Organization.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Organization.Infrastructure;

/// <summary>Organization trả dữ liệu phòng ban qua projection, không để caller nắm entity Department.</summary>
public sealed class OrganizationDirectory(AppDbContext db) : IOrganizationDirectory
{
    public Task<bool> IsMaintenanceTeamAsync(int departmentId, CancellationToken cancellationToken = default) =>
        db.Departments.AnyAsync(
            department => department.Id == departmentId && department.IsMaintenanceTeam,
            cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        db.Departments.CountAsync(cancellationToken);

    public Task<int?> GetDefaultDepartmentIdAsync(CancellationToken cancellationToken = default) =>
        db.Departments
            .Select(department => (int?)department.Id)
            .FirstOrDefaultAsync(cancellationToken);

    // Projection này giữ Department entity bên trong Organization; module gọi chỉ nhận đúng ba trường công khai.
    public Task<OrganizationDepartmentInfo?> FindAsync(
        int? departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!departmentId.HasValue)
            return Task.FromResult<OrganizationDepartmentInfo?>(null);

        return db.Departments
            .AsNoTracking()
            .Where(department => department.Id == departmentId.Value)
            .Select(department => new OrganizationDepartmentInfo(
                department.Id,
                department.Name,
                department.IsMaintenanceTeam))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OrganizationDepartmentInfo?> FindMaintenanceTeamOrDefaultAsync(
        CancellationToken cancellationToken = default) =>
        db.Departments
            .AsNoTracking()
            .OrderByDescending(department => department.IsMaintenanceTeam)
            .Select(department => new OrganizationDepartmentInfo(
                department.Id,
                department.Name,
                department.IsMaintenanceTeam))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, string>> FindNamesAsync(
        IReadOnlyCollection<int> departmentIds,
        CancellationToken cancellationToken = default)
    {
        if (departmentIds.Count == 0)
            return new Dictionary<int, string>();

        return await db.Departments
            .AsNoTracking()
            .Where(department => departmentIds.Contains(department.Id))
            .ToDictionaryAsync(department => department.Id, department => department.Name, cancellationToken);
    }
}
