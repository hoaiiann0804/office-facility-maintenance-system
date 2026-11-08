namespace InternalMaintenance.Maintenance.Contracts.Reporting;

/// <summary>Tiêu chí đọc SLA từ Reporting, bao gồm phạm vi quyền đã được xác định ở module gọi.</summary>
public sealed record SlaComplianceReadQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    int? DepartmentId,
    int? TechnicianId,
    bool IsAdministrator,
    int? ManagerDepartmentId,
    bool CanViewReport,
    DateTime NowUtc);

/// <summary>Kết quả nhóm theo SLA, priority và tháng; các phép đếm đã chạy trong SQL Server.</summary>
public sealed record SlaComplianceReadResult(
    int TotalTickets,
    int InSlaActiveCount,
    int NearBreachCount,
    int BreachedActiveCount,
    int PausedCount,
    int MetSlaCount,
    int MissedSlaCount,
    IReadOnlyCollection<SlaPriorityReadRow> PriorityRows,
    IReadOnlyCollection<SlaMonthlyReadRow> MonthlyRows);

public sealed record SlaPriorityReadRow(string Priority, int Total, int Met, int Missed);

public sealed record SlaMonthlyReadRow(int Year, int Month, int Met, int Missed);

/// <summary>
/// Maintenance thực thi ticket/SLA read query để Reporting không cần tham chiếu entity Domain.
/// </summary>
public interface ISlaComplianceReadStore
{
    Task<SlaComplianceReadResult> ReadAsync(
        SlaComplianceReadQuery query,
        CancellationToken cancellationToken = default);
}
