namespace InternalMaintenance.Organization.Contracts;

/// <summary>Projection phòng ban cho module khác; không để entity Department đi qua ranh giới module.</summary>
public interface IOrganizationDirectory
{
    Task<bool> IsMaintenanceTeamAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int?> GetDefaultDepartmentIdAsync(CancellationToken cancellationToken = default);
    Task<OrganizationDepartmentInfo?> FindAsync(int? departmentId, CancellationToken cancellationToken = default);
    Task<OrganizationDepartmentInfo?> FindMaintenanceTeamOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> FindNamesAsync(IReadOnlyCollection<int> departmentIds, CancellationToken cancellationToken = default);
}

public sealed record OrganizationDepartmentInfo(int Id, string Name, bool IsMaintenanceTeam);
