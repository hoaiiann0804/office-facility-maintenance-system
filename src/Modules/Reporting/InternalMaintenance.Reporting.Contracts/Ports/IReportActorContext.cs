namespace InternalMaintenance.Reporting.Contracts;

/// <summary>
/// Cung cấp phạm vi quyền của request mà không buộc module Reporting tham chiếu API host.
/// Host triển khai port này từ claims JWT; báo cáo vẫn tự lọc dữ liệu theo phòng ban ở tầng truy vấn.
/// </summary>
public interface IReportActorContext
{
    string Role { get; }
    int? DepartmentId { get; }
}
