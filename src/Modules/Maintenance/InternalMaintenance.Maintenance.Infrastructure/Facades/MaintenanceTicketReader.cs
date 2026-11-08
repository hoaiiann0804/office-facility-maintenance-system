using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.Common;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketById;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketComments;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketHistory;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketsPage;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BỘ ĐIỀU PHỐI MỎNG CHO CÁC LUỒNG ĐỌC PHIẾU BẢO TRÌ (READ-SIDE)]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    MaintenanceTicketsController -> IMaintenanceTicketReader (MaintenanceTicketReader)
///      ├── GetPageAsync(...)      ---> GetTicketsPageHandler.HandleAsync(...)
///      ├── GetByIdAsync(...)      ---> GetTicketByIdHandler.HandleAsync(...)
///      ├── GetCommentsAsync(...)  ---> GetTicketCommentsHandler.HandleAsync(...)
///      └── GetHistoryAsync(...)   ---> GetTicketHistoryHandler.HandleAsync(...)
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách 4 luồng đọc có phân quyền của MaintenanceTicketReader thành 4 Vertical Slice Handlers độc lập.
///    - Dùng chung `TicketVisibilityFilter` để đảm bảo quy tắc RBAC (Admin/Manager/Technician/Staff) nhất quán.
///    - Giữ constructor tương thích ngược phục vụ các bài kiểm thử hiện có.
/// </summary>
public sealed class MaintenanceTicketReader : IMaintenanceTicketReader
{
    private readonly GetTicketsPageHandler _getTicketsPageHandler;
    private readonly GetTicketByIdHandler _getTicketByIdHandler;
    private readonly GetTicketCommentsHandler _getTicketCommentsHandler;
    private readonly GetTicketHistoryHandler _getTicketHistoryHandler;

    [ActivatorUtilitiesConstructor]
    public MaintenanceTicketReader(
        GetTicketsPageHandler getTicketsPageHandler,
        GetTicketByIdHandler getTicketByIdHandler,
        GetTicketCommentsHandler getTicketCommentsHandler,
        GetTicketHistoryHandler getTicketHistoryHandler)
    {
        _getTicketsPageHandler = getTicketsPageHandler;
        _getTicketByIdHandler = getTicketByIdHandler;
        _getTicketCommentsHandler = getTicketCommentsHandler;
        _getTicketHistoryHandler = getTicketHistoryHandler;
    }

    public MaintenanceTicketReader(
        AppDbContext db,
        IIdentityUserDirectory identityDirectory,
        IEquipmentTicketContextReader equipmentReader,
        IVendorDirectory vendorDirectory)
        : this(
            new GetTicketsPageHandler(
                db,
                new TicketVisibilityFilter(identityDirectory, equipmentReader),
                identityDirectory,
                equipmentReader,
                vendorDirectory),
            new GetTicketByIdHandler(
                db,
                new TicketVisibilityFilter(identityDirectory, equipmentReader),
                identityDirectory,
                equipmentReader,
                vendorDirectory),
            new GetTicketCommentsHandler(
                db,
                new TicketVisibilityFilter(identityDirectory, equipmentReader),
                identityDirectory),
            new GetTicketHistoryHandler(
                db,
                new TicketVisibilityFilter(identityDirectory, equipmentReader),
                identityDirectory))
    {
    }

    public Task<PagedResponse<MaintenanceTicketResponse>> GetPageAsync(
        TicketQuery query,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default) =>
        _getTicketsPageHandler.HandleAsync(query, actor, cancellationToken);

    public Task<MaintenanceTicketResponse?> GetByIdAsync(
        int ticketId,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default) =>
        _getTicketByIdHandler.HandleAsync(ticketId, actor, cancellationToken);

    public Task<IReadOnlyList<TicketCommentResponse>?> GetCommentsAsync(
        int ticketId,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default) =>
        _getTicketCommentsHandler.HandleAsync(ticketId, actor, cancellationToken);

    public Task<IReadOnlyList<TicketStatusHistoryResponse>?> GetHistoryAsync(
        int ticketId,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default) =>
        _getTicketHistoryHandler.HandleAsync(ticketId, actor, cancellationToken);
}
