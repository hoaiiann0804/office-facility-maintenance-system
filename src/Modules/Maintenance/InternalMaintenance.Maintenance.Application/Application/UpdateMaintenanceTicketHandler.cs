using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Domain;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// [VERTICAL SLICE WRITE HANDLER: UPDATE MAINTENANCE TICKET - CẬP NHẬT THÔNG TIN &amp; TÍNH LẠI HẠN SLA]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> PUT /api/tickets/{id} (MaintenanceTicketsController)
///           -> UpdateMaintenanceTicketHandler.Handle(command, cancellationToken)
///           -> Bước 1: Tải Ticket và chặn sửa nếu phiếu đã kết thúc (`Closed`, `Resolved`, `Cancelled`).
///           -> Bước 2 (Quy tắc phân quyền chỉnh sửa):
///              + Nếu người gọi là người tạo phiếu (`CreatedByUserId == ActorUserId`): Chỉ được sửa khi phiếu còn ở `Pending`.
///              + Nếu người gọi là `Admin` hoặc `Manager` có thẩm quyền trên thiết bị/phòng ban: Được phép cập nhật.
///           -> Bước 3 (Domain SLA Recalculation):
///              Nếu `Priority` thay đổi, gọi `ticket.ChangePriority(priority, UtcNow)` để Aggregate tự tính lại `DueAt`
///              mới (cộng bù thời gian đã tạm dừng chờ Vendor `TotalSlaPausedMinutes` và xóa cờ cảnh báo SLA cũ).
///           -> Bước 4: Lưu thay đổi qua `ITicketUpdateStore.SaveChangesAsync` và trả về DTO phản hồi.
/// </summary>
public sealed class UpdateMaintenanceTicketHandler(
    ITicketUpdateStore store,
    IEquipmentTicketContextReader equipmentReader,
    IIdentityUserDirectory identityDirectory)
{
    public async Task<TicketUpdateOutcome> Handle(
        UpdateMaintenanceTicketCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await store.GetForUpdateAsync(command.TicketId, cancellationToken);
        if (ticket is null)
            return Fail(TicketUpdateError.NotFound, "Ticket Not Found");

        if (ticket.Status is TicketStatuses.Closed or TicketStatuses.Resolved or TicketStatuses.Cancelled)
            return Invalid("Cannot edit a finalized ticket");

        // Giữ nguyên ưu tiên của quy tắc cũ: người tạo ticket chỉ được sửa ticket Pending,
        // kể cả khi người đó đồng thời mang role Admin/Manager.
        if (ticket.CreatedByUserId == command.ActorUserId)
        {
            if (ticket.Status != TicketStatuses.Pending)
                return Invalid("Ticket already in processing state");
        }
        else
        {
            if (command.ActorRole is not (UserRoles.Admin or UserRoles.Manager))
                return Fail(TicketUpdateError.Forbidden, "Forbidden");

            if (command.ActorRole == UserRoles.Manager &&
                !await ManagerCanAccessAsync(ticket.EquipmentId, ticket.CreatedByUserId,
                    command.ActorUserId, command.ActorDepartmentId, cancellationToken))
                return Fail(TicketUpdateError.Forbidden, "Forbidden");
        }

        var priority = command.Priority.Trim();
        if (!TicketWorkflowRules.IsAllowedPriority(priority))
            return Invalid("Invalid priority");

        // API model chưa đủ để bảo vệ use case khi được gọi từ job/test/module khác.
        // Kiểm tra trước khi sửa priority để lệnh sai không làm aggregate đang được EF track đổi dở.
        if (string.IsNullOrWhiteSpace(command.Title))
            return Invalid("Ticket title is required");
        if (string.IsNullOrWhiteSpace(command.Description))
            return Invalid("Ticket description is required");

        if (ticket.Priority != priority)
        {
            // Priority làm đổi ngân sách SLA. Entity tự tính hạn mới, cộng lại thời gian vendor
            // đã pause và xóa cảnh báo cũ để deadline mới không kế thừa nhãn SLA lỗi thời.
            ticket.ChangePriority(priority, DateTime.UtcNow);
        }

        ticket.UpdateDetails(command.Title, command.Description);

        await store.SaveChangesAsync(cancellationToken);
        var response = await store.GetResponseAsync(ticket.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Ticket {ticket.Id} was updated but its response could not be loaded.");

        return TicketUpdateOutcome.Success(response);
    }

    private async Task<bool> ManagerCanAccessAsync(
        int equipmentId,
        int requesterId,
        int managerId,
        int? managerDepartmentId,
        CancellationToken cancellationToken)
    {
        if (!managerDepartmentId.HasValue)
            return false;

        var equipment = await equipmentReader.GetAsync(equipmentId, cancellationToken);
        if (equipment is not null &&
            (equipment.DepartmentId == managerDepartmentId ||
             equipment.MaintenanceDepartmentId == managerDepartmentId))
            return true;

        if (requesterId == managerId)
            return true;

        // Nếu thiết bị không thuộc phạm vi Manager, quyền có thể đến từ phòng ban của requester.
        // Hỏi Identity qua contract thay vì đọc User/navigation chéo trong module Maintenance.
        return await identityDirectory.FindDepartmentIdAsync(requesterId, cancellationToken) == managerDepartmentId;
    }

    private static TicketUpdateOutcome Invalid(string message) =>
        Fail(TicketUpdateError.InvalidRequest, message);

    private static TicketUpdateOutcome Fail(TicketUpdateError error, string message) =>
        TicketUpdateOutcome.Fail(error, message);
}
