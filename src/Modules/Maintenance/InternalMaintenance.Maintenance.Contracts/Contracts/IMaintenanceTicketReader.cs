using InternalMaintenance.BuildingBlocks.Pagination;

namespace InternalMaintenance.Maintenance.Contracts;

// Đây là cổng đọc công khai của Maintenance: API chỉ nhận projection, không cần tham chiếu EF hay entity nội bộ.
/// <summary>
/// Contract đọc do Maintenance sở hữu. API chỉ nhận projection, không truy vấn DbSet
/// và không phụ thuộc entity EF nội bộ của module.
/// </summary>
public interface IMaintenanceTicketReader
{
    Task<PagedResponse<MaintenanceTicketResponse>> GetPageAsync(
        TicketQuery query,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default);

    Task<MaintenanceTicketResponse?> GetByIdAsync(
        int ticketId,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketCommentResponse>?> GetCommentsAsync(
        int ticketId,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketStatusHistoryResponse>?> GetHistoryAsync(
        int ticketId,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default);
}
