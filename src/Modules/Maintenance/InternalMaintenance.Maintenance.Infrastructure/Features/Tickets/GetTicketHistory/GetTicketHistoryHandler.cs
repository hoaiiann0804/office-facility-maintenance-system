using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.Common;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketHistory;

/// <summary>
/// [VERTICAL SLICE: GET TICKET HISTORY - LẤY DÒNG THỜI GIAN LỊCH SỬ ĐỔI TRẠNG THÁI PHIẾU BẢO TRÌ]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/tickets/{id}/history (MaintenanceTicketsController)
///           -> IMaintenanceTicketReader.GetHistoryAsync(ticketId, actor)
///           -> GetTicketHistoryHandler.HandleAsync(ticketId, actor)
///           -> Bước 1: Kiểm tra quyền xem Ticket qua `TicketVisibilityFilter`.
///           -> Bước 2: Truy vấn `TicketStatusHistories` theo `ticketId` (`AsNoTracking`), sắp xếp theo `ChangedAt`.
///           -> Bước 3: Gọi `IIdentityUserDirectory.FindDisplayNamesAsync` theo lô để điền `ChangedByUserName`.
/// </summary>
public sealed class GetTicketHistoryHandler(
    AppDbContext db,
    TicketVisibilityFilter visibilityFilter,
    IIdentityUserDirectory identityDirectory)
{
    public async Task<IReadOnlyList<TicketStatusHistoryResponse>?> HandleAsync(
        int ticketId,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default)
    {
        var visibleTickets = await visibilityFilter.ApplyVisibilityAsync(
            db.MaintenanceTickets.AsNoTracking(),
            actor,
            cancellationToken);

        if (!await visibleTickets.AnyAsync(ticket => ticket.Id == ticketId, cancellationToken))
            return null;

        var histories = await db.TicketStatusHistories
            .AsNoTracking()
            .Where(history => history.MaintenanceTicketId == ticketId)
            .OrderBy(history => history.ChangedAt)
            .Select(history => new TicketStatusHistoryResponse
            {
                Id = history.Id,
                MaintenanceTicketId = history.MaintenanceTicketId,
                OldStatus = history.OldStatus,
                NewStatus = history.NewStatus,
                ChangedByUserId = history.ChangedByUserId,
                ChangedAt = history.ChangedAt,
                Note = history.Note
            })
            .ToListAsync(cancellationToken);

        var names = await identityDirectory.FindDisplayNamesAsync(
            histories.Select(history => history.ChangedByUserId).Distinct().ToArray(),
            cancellationToken);

        foreach (var history in histories)
            history.ChangedByUserName = names.GetValueOrDefault(history.ChangedByUserId, string.Empty);

        return histories;
    }
}

