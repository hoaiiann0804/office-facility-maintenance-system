namespace InternalMaintenance.Assets.Contracts;

public sealed record EquipmentDashboardStatistics(
    int TotalEquipment,
    int ActiveEquipment,
    int UnderMaintenanceEquipment,
    IReadOnlyCollection<EquipmentDepartmentCount> EquipmentByDepartment);

public sealed record EquipmentDepartmentCount(int DepartmentId, int Count);

/// <summary>Assets sở hữu phép đếm/nhóm thiết bị để Dashboard không truy vấn bảng Equipment trực tiếp.</summary>
public interface IEquipmentDashboardReader
{
    Task<IReadOnlyCollection<int>> FindEquipmentIdsByDepartmentAsync(
        int? departmentId,
        CancellationToken cancellationToken = default);

    Task<EquipmentDashboardStatistics> GetAsync(
        int? departmentId,
        bool restrictToDepartment,
        CancellationToken cancellationToken = default);
}
