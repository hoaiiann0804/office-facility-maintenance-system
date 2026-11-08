namespace InternalMaintenance.Maintenance.Contracts.Reporting;

/// <summary>Phạm vi đọc chi phí vendor đã được Reporting xác định theo actor và filter.</summary>
public sealed record MaintenanceCostReadQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    int? DepartmentId,
    bool IsAdministrator,
    int? ManagerDepartmentId,
    bool CanViewReport,
    DateTime NowUtc);

public sealed record MonthlyMaintenanceCostReadRow(int Year, int Month, decimal TotalCost, int TicketCount);
public sealed record VendorMaintenanceCostReadRow(int VendorId, decimal TotalCost, int DispatchCount);
public sealed record EquipmentMaintenanceCostReadRow(
    int EquipmentId,
    string EquipmentCode,
    string EquipmentName,
    int DepartmentId,
    string? DepartmentName,
    decimal TotalRepairCost,
    int MaintenanceCount);
public sealed record DepartmentMaintenanceCostReadRow(
    int DepartmentId,
    string? DepartmentName,
    decimal TotalCost,
    int TicketCount);

/// <summary>
/// Kết quả tổng hợp; ID Vendor được giữ lại để Reporting tra tên qua public contract của Vendors.
/// </summary>
public sealed record MaintenanceCostReadResult(
    decimal TotalCost,
    int TotalDispatches,
    int DistinctTicketCount,
    IReadOnlyCollection<MonthlyMaintenanceCostReadRow> MonthlyCosts,
    IReadOnlyCollection<VendorMaintenanceCostReadRow> VendorCosts,
    IReadOnlyCollection<EquipmentMaintenanceCostReadRow> TopEquipment,
    IReadOnlyCollection<DepartmentMaintenanceCostReadRow> DepartmentCosts);

/// <summary>Maintenance sở hữu ticket/vendor log và cung cấp kết quả đọc đã tổng hợp cho Reporting.</summary>
public interface IMaintenanceCostReadStore
{
    Task<MaintenanceCostReadResult> ReadAsync(
        MaintenanceCostReadQuery query,
        CancellationToken cancellationToken = default);
}
