namespace InternalMaintenance.Maintenance.Contracts;

/// <summary>Phạm vi đọc Dashboard mà Maintenance áp dụng trực tiếp lên dữ liệu ticket.</summary>
public sealed record TicketDashboardScope(
    int? CreatedByUserId = null,
    int? AssignedTechnicianId = null,
    bool MatchCreatedOrAssigned = false,
    IReadOnlyCollection<int>? EquipmentIds = null,
    bool RestrictToEquipmentIds = false);

public sealed record DashboardCount(string Name, int Value);

public sealed record MaintenanceDashboardStatistics(
    int TotalTickets,
    int OpenTickets,
    int ResolvedTickets,
    int ClosedTickets,
    IReadOnlyCollection<DashboardCount> TicketsByStatus,
    IReadOnlyCollection<DashboardCount> TicketsByPriority);

/// <summary>Maintenance sở hữu truy vấn tổng hợp trên ticket; caller không cần biết DbSet hay entity.</summary>
public interface IMaintenanceDashboardReader
{
    Task<MaintenanceDashboardStatistics> GetAsync(
        TicketDashboardScope scope,
        CancellationToken cancellationToken = default);
}
