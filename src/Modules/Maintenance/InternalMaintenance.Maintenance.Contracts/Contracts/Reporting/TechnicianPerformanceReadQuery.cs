namespace InternalMaintenance.Maintenance.Contracts.Reporting;

/// <summary>
/// Tiêu chí đọc số liệu technician do Reporting gửi sang module sở hữu ticket.
/// </summary>
public sealed record TechnicianPerformanceReadQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    int? DepartmentId,
    int? TechnicianId,
    bool IsAdministrator,
    int? ManagerDepartmentId,
    bool CanViewReport);

/// <summary>
/// Dữ liệu đã tổng hợp phía SQL; Reporting chỉ chuyển thành response và tính tỷ lệ hiển thị.
/// </summary>
public sealed record TechnicianPerformanceReadRow(
    int TechnicianId,
    string FullName,
    string Email,
    string? DepartmentName,
    int TotalAssigned,
    int InProgressCount,
    int WaitingForVendorCount,
    int ResolvedCount,
    int ClosedCount,
    int MetSlaCount,
    int MissedSlaCount,
    double? AverageResolutionSeconds);

/// <summary>
/// Read contract để Reporting lấy số liệu từ Maintenance mà không cần tham chiếu entity Domain.
/// </summary>
public interface ITechnicianPerformanceReadStore
{
    Task<IReadOnlyCollection<TechnicianPerformanceReadRow>> ReadAsync(
        TechnicianPerformanceReadQuery query,
        CancellationToken cancellationToken = default);
}
