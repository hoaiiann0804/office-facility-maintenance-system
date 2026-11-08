using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// [VERTICAL SLICE WRITE HANDLER: CREATE TICKET COMMENT - THÊM BÌNH LUẬN VÀO PHIẾU BẢO TRÌ]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/tickets/{id}/comments (MaintenanceTicketsController)
///           -> CreateTicketCommentHandler.Handle(command, cancellationToken)
///           -> Bước 1: Kiểm tra Ticket tồn tại và chưa bị đóng/hủy (`Closed` / `Cancelled`).
///           -> Bước 2: Kiểm tra quyền tham gia bình luận của Actor (Admin, Manager cùng phòng ban,
///              Technician được giao việc, hoặc Staff tạo phiếu).
///           -> Bước 3: Lấy tên hiển thị người bình luận qua `IIdentityUserDirectory.FindDisplayNameAsync`
///              và lưu bình luận qua `ITicketCommentStore.AddAsync`.
/// </summary>
public sealed class CreateTicketCommentHandler(
    ITicketCommentStore store,
    IEquipmentTicketContextReader equipmentReader,
    IIdentityUserDirectory identityDirectory)
{
    public async Task<TicketCommentOutcome> Handle(
        CreateTicketCommentCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await store.GetTicketContextAsync(command.TicketId, cancellationToken);
        if (ticket is null)
            return TicketCommentOutcome.Fail(TicketCommentError.NotFound, "Ticket not found");

        if (ticket.Status is TicketStatuses.Closed or TicketStatuses.Cancelled)
            return TicketCommentOutcome.Fail(TicketCommentError.InvalidState,
                "Cannot comment on finalized ticket");

        if (!await CanAccessAsync(ticket, command, cancellationToken))
            return TicketCommentOutcome.Fail(TicketCommentError.Forbidden, "Forbidden");

        // Tên người dùng thuộc Identity; chỉ lấy qua contract và không gắn navigation User vào Tickets.
        var userName = await identityDirectory.FindDisplayNameAsync(command.ActorUserId, cancellationToken);
        if (userName is null)
            return TicketCommentOutcome.Fail(TicketCommentError.UserNotFound, "Current user was not found");

        var content = command.Content.Trim();
        var createdAt = DateTime.UtcNow;
        var commentId = await store.AddAsync(
            ticket.TicketId,
            command.ActorUserId,
            content,
            createdAt,
            cancellationToken);

        return TicketCommentOutcome.Success(new TicketCommentResponse
        {
            Id = commentId,
            UserId = command.ActorUserId,
            UserName = userName,
            Content = content,
            CreatedAt = createdAt
        });
    }

    private async Task<bool> CanAccessAsync(
        MaintenanceTicketAccessContext ticket,
        CreateTicketCommentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorRole == UserRoles.Admin)
            return true;
        if (command.ActorRole == UserRoles.Staff)
            return ticket.CreatedByUserId == command.ActorUserId;
        if (command.ActorRole == UserRoles.Technician)
            return ticket.CreatedByUserId == command.ActorUserId ||
                   ticket.AssignedTechnicianId == command.ActorUserId ||
                   ticket.SupportTechnicianId == command.ActorUserId;
        if (command.ActorRole != UserRoles.Manager || !command.ActorDepartmentId.HasValue)
            return false;

        var equipment = await equipmentReader.GetAsync(ticket.EquipmentId, cancellationToken);
        if (equipment is not null &&
            (equipment.DepartmentId == command.ActorDepartmentId ||
             equipment.MaintenanceDepartmentId == command.ActorDepartmentId))
            return true;

        var requesterDepartmentId = await identityDirectory.FindDepartmentIdAsync(
            ticket.CreatedByUserId,
            cancellationToken);
        return requesterDepartmentId == command.ActorDepartmentId;
    }
}
