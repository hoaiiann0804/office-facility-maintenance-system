using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.Common;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketComments;

/// <summary>
/// [VERTICAL SLICE: GET TICKET COMMENTS - LẤY DANH SÁCH BÌNH LUẬN TRÊN PHIẾU BẢO TRÌ]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/tickets/{id}/comments (MaintenanceTicketsController)
///           -> IMaintenanceTicketReader.GetCommentsAsync(ticketId, actor)
///           -> GetTicketCommentsHandler.HandleAsync(ticketId, actor)
///           -> Bước 1: Kiểm tra người dùng có quyền nhìn thấy Ticket này không qua `TicketVisibilityFilter`.
///           -> Bước 2: Truy vấn danh sách `TicketComments` theo `ticketId` (`AsNoTracking`), sắp xếp theo `CreatedAt`.
///           -> Bước 3: Gọi `IIdentityUserDirectory.FindDisplayNamesAsync` theo lô để điền `UserName` người bình luận.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Slice đọc bình luận này không cần phụ thuộc vào `IVendorDirectory` (vốn chỉ dùng cho danh sách/chi tiết Ticket).
/// </summary>
public sealed class GetTicketCommentsHandler(
    AppDbContext db,
    TicketVisibilityFilter visibilityFilter,
    IIdentityUserDirectory identityDirectory)
{
    public async Task<IReadOnlyList<TicketCommentResponse>?> HandleAsync(
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

        var comments = await db.TicketComments
            .AsNoTracking()
            .Where(comment => comment.MaintenanceTicketId == ticketId)
            .OrderBy(comment => comment.CreatedAt)
            .Select(comment => new TicketCommentResponse
            {
                Id = comment.Id,
                UserId = comment.UserId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var names = await identityDirectory.FindDisplayNamesAsync(
            comments.Select(comment => comment.UserId).Distinct().ToArray(),
            cancellationToken);

        foreach (var comment in comments)
            comment.UserName = names.GetValueOrDefault(comment.UserId, string.Empty);

        return comments;
    }
}

