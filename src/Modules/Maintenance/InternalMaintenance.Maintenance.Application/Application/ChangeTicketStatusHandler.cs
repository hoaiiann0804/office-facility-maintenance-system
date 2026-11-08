using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Vendors.Contracts;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// [VERTICAL SLICE WRITE HANDLER: CHANGE TICKET STATUS - MÁY TRẠNG THÁI VÒNG ĐỜI PHIẾU BẢO TRÌ &amp; SLA PAUSE/RESUME]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> PATCH /api/tickets/{id}/status (MaintenanceTicketsController)
///           -> ChangeTicketStatusHandler.Handle(command, cancellationToken)
///           -> Bước 1: Tải Ticket và thông tin thiết bị (`IEquipmentTicketContextReader`).
///           -> Bước 2: Kiểm tra quy tắc chuyển trạng thái qua Máy trạng thái `TicketWorkflowRules.CanTransition(oldStatus, newStatus)`.
///           -> Bước 3 (Xử lý nghiệp vụ theo từng trạng thái đích trên Domain Aggregate `MaintenanceTicket`):
///              + `WaitingForVendor`: Kiểm tra Vendor đang hoạt động qua `IVendorDirectory.IsActiveAsync`, tạm dừng đồng hồ SLA
///                (`ticket.SendToVendor`) và tạo bản ghi `TicketVendorLog` chốt phòng ban tại thời điểm gửi.
///              + `WaitingForVendor -> InProgress`: Đóng `TicketVendorLog`, tính số phút tạm dừng (`PausedMinutes`)
///                và cộng bù vào hạn SLA (`ticket.ReturnFromVendor`).
///              + `InProgress`: Bắt đầu xử lý (`ticket.Start`) và chuyển trạng thái thiết bị sang `UnderMaintenance`
///                qua `IEquipmentMaintenanceGateway.SetStatusAsync`.
///              + `Resolved`: Ghi nhận kết quả sửa chữa (`ticket.Resolve`) và chốt kết quả SLA (`MetSLA` / `Breached`).
///              + `Closed` / `Cancelled`: Đóng/Hủy phiếu và trả trạng thái thiết bị về `Active` qua `IEquipmentMaintenanceGateway`.
///           -> Bước 4: Ghi `TicketStatusHistory` và gọi `SaveChangesAsync()` 1 lần duy nhất (Atomic Unit of Work).
/// </summary>
public sealed class ChangeTicketStatusHandler(
    ITicketStatusChangeStore store,
    IEquipmentMaintenanceGateway equipmentGateway,
    IEquipmentTicketContextReader equipmentReader,
    IIdentityUserDirectory identityUserDirectory,
    IVendorDirectory vendorDirectory)
{
    private static readonly string[] ClientTransitionStatuses =
    [
        TicketStatuses.InProgress,
        TicketStatuses.Resolved,
        TicketStatuses.Closed,
        TicketStatuses.Cancelled,
        TicketStatuses.WaitingForVendor
    ];

    public async Task<TicketStatusChangeOutcome> Handle(
        ChangeTicketStatusCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await store.GetForStatusChangeAsync(command.TicketId, cancellationToken);
        if (ticket is null)
            return TicketStatusChangeOutcome.Fail(TicketStatusChangeError.NotFound, "Ticket not found");

        // Equipment và Identity sở hữu dữ liệu phòng ban. Maintenance lấy projection qua
        // contract để kiểm tra quyền mà không Include navigation chéo vào aggregate Ticket.
        var equipment = await equipmentReader.GetAsync(ticket.EquipmentId, cancellationToken);
        if (equipment is null)
            return TicketStatusChangeOutcome.Fail(TicketStatusChangeError.NotFound, "Equipment not found");

        var newStatus = command.NewStatus?.Trim();
        if (string.IsNullOrWhiteSpace(newStatus) || !ClientTransitionStatuses.Contains(newStatus))
            return Invalid("Invalid ticket status");

        if (ticket.Status is TicketStatuses.Closed or TicketStatuses.Cancelled)
            return Invalid("Finalized ticket cannot be updated");

        if (!TicketWorkflowRules.CanTransition(ticket.Status, newStatus))
            return Invalid($"Cannot change ticket from {ticket.Status} to {newStatus}");

        if (newStatus == TicketStatuses.InProgress && ticket.AssignedTechnicianId is null)
            return Invalid("Ticket must be assigned before moving to InProgress");

        if (RequiresAssignedTechnician(newStatus) &&
            command.ActorRole is not (UserRoles.Admin or UserRoles.Manager) &&
            ticket.AssignedTechnicianId != command.ActorUserId)
        {
            return TicketStatusChangeOutcome.Fail(TicketStatusChangeError.Forbidden, "Forbidden");
        }

        if (newStatus == TicketStatuses.Resolved && string.IsNullOrWhiteSpace(command.ResolutionNote))
            return Invalid("Resolution note is required when resolving a ticket");

        var cancellationReason = string.IsNullOrWhiteSpace(command.CancellationReason)
            ? command.Note
            : command.CancellationReason;
        if (newStatus == TicketStatuses.Cancelled && string.IsNullOrWhiteSpace(cancellationReason))
            return Invalid("Cancellation reason is required when cancelling a ticket");

        // Kiểm tra quyền trước khi sửa entity đang được EF track. Request bị từ chối
        // không được để lại thay đổi có thể bị SaveChanges của luồng khác ghi nhầm.
        if ((newStatus is TicketStatuses.Closed or TicketStatuses.Cancelled) &&
            !await CanCloseOrCancelAsync(ticket, equipment, command, cancellationToken))
            return TicketStatusChangeOutcome.Fail(TicketStatusChangeError.Forbidden, "Forbidden");

        var oldStatus = ticket.Status;
        var changedAt = DateTime.UtcNow;

        if (newStatus == TicketStatuses.WaitingForVendor)
        {
            if (!command.VendorId.HasValue || !command.VendorEstimatedReturnDate.HasValue)
                return Invalid("Vendor ID and Estimated Return Date are required when waiting for vendor");

            if (command.VendorEstimatedReturnDate.Value.Date < changedAt.Date)
                return Invalid("Estimated Return Date cannot be in the past");

            // Kiểm tra qua contract của Vendors trước khi sửa Ticket. Điều này ngăn gán vendor
            // đã ngừng hoạt động, đồng thời giữ Maintenance độc lập với entity/DbSet của Vendors.
            if (!await vendorDirectory.IsActiveAsync(command.VendorId.Value, cancellationToken))
                return Invalid("Vendor does not exist or is inactive");

            ticket.SendToVendor(changedAt);
            store.AddVendorLog(new TicketVendorLog
            {
                MaintenanceTicketId = ticket.Id,
                VendorId = command.VendorId.Value,
                // Chốt phòng sở hữu và phòng chịu trách nhiệm bảo trì tại đúng thời điểm dispatch;
                // báo cáo cũ không bị đổi khi quản trị viên điều chuyển thiết bị sang phòng khác.
                DepartmentIdAtDispatch = equipment.DepartmentId,
                MaintenanceDepartmentIdAtDispatch = equipment.MaintenanceDepartmentId,
                DispatchedAt = changedAt,
                EstimatedReturnDate = command.VendorEstimatedReturnDate.Value,
                Note = command.VendorNote?.Trim()
            });
        }

        if (oldStatus == TicketStatuses.WaitingForVendor && newStatus == TicketStatuses.InProgress)
        {
            var activeLog = await store.GetOpenVendorLogAsync(ticket.Id, cancellationToken);
            var pausedMinutes = 0;
            if (activeLog is not null)
            {
                activeLog.ActualReturnDate = changedAt;
                activeLog.PausedMinutes = (int)(changedAt - activeLog.DispatchedAt).TotalMinutes;
                pausedMinutes = activeLog.PausedMinutes;
            }

            ticket.ReturnFromVendor(changedAt, pausedMinutes);
        }
        else if (newStatus == TicketStatuses.InProgress)
        {
            ticket.Start(changedAt);
        }
        else if (newStatus == TicketStatuses.Resolved)
        {
            ticket.Resolve(command.ResolutionNote!, changedAt);
        }
        else if (newStatus == TicketStatuses.Closed)
        {
            ticket.Close(changedAt);
        }
        else if (newStatus == TicketStatuses.Cancelled)
        {
            ticket.Cancel(cancellationReason!);
        }

        if (newStatus == TicketStatuses.InProgress)
            await equipmentGateway.SetStatusAsync(ticket.EquipmentId, EquipmentStatuses.UnderMaintenance, cancellationToken);
        else if (newStatus is TicketStatuses.Closed or TicketStatuses.Cancelled)
            await equipmentGateway.SetStatusAsync(ticket.EquipmentId, EquipmentStatuses.Active, cancellationToken);

        store.AddStatusHistory(new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = command.ActorUserId,
            ChangedAt = changedAt,
            Note = command.Note?.Trim()
        });

        // Chỉ commit một lần để ticket, equipment, vendor log và history cùng thành công
        // hoặc cùng rollback; store và gateway đang dùng chung scoped AppDbContext.
        await store.SaveChangesAsync(cancellationToken);

        var response = await store.GetResponseAsync(ticket.Id, cancellationToken);
        return response is null
            ? TicketStatusChangeOutcome.Fail(TicketStatusChangeError.NotFound, "Ticket not found after update")
            : TicketStatusChangeOutcome.Success(response);
    }

    private static bool RequiresAssignedTechnician(string status) =>
        status is TicketStatuses.InProgress or TicketStatuses.Resolved or TicketStatuses.WaitingForVendor;

    private async Task<bool> CanCloseOrCancelAsync(
        MaintenanceTicket ticket,
        EquipmentTicketContext equipment,
        ChangeTicketStatusCommand actor,
        CancellationToken cancellationToken)
    {
        if (actor.ActorRole == UserRoles.Admin || ticket.CreatedByUserId == actor.ActorUserId)
            return true;

        if (actor.ActorRole != UserRoles.Manager || actor.ActorDepartmentId is null)
            return false;

        if (equipment.DepartmentId == actor.ActorDepartmentId ||
            equipment.MaintenanceDepartmentId == actor.ActorDepartmentId)
            return true;

        // Chỉ cần đọc phòng ban người tạo ở nhánh Manager chưa khớp với hai phòng ban Assets.
        // Như vậy Identity giữ quyền sở hữu dữ liệu User mà không phải Include navigation trong mọi request.
        var requesterDepartmentId = await identityUserDirectory.FindDepartmentIdAsync(
            ticket.CreatedByUserId,
            cancellationToken);
        return requesterDepartmentId == actor.ActorDepartmentId;
    }

    private static TicketStatusChangeOutcome Invalid(string message) =>
        TicketStatusChangeOutcome.Fail(TicketStatusChangeError.InvalidRequest, message);
}
