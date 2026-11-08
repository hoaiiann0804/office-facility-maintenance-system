using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Domain;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// [VERTICAL SLICE WRITE HANDLER: ASSIGN MAINTENANCE TICKET - PHÂN CÔNG KỸ THUẬT VIÊN CHÍNH &amp; PHỤ]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> PUT /api/tickets/{id}/assign (MaintenanceTicketsController)
///           -> AssignMaintenanceTicketHandler.Handle(command, cancellationToken)
///           -> Bước 1: Tải Ticket qua `ITicketAssignmentStore.GetForAssignmentAsync`.
///           -> Bước 2 (Kiểm tra quyền phân công liên module):
///              Hỏi `IEquipmentTicketContextReader.GetAsync` để biết thiết bị thuộc `MaintenanceDepartmentId` nào.
///              Chỉ `Admin` hoặc `Manager` của đúng phòng bảo trì đó mới được phép phân công.
///           -> Bước 3: Kiểm tra trạng thái Ticket hợp lệ (`TicketWorkflowRules.IsAssignableStatus`).
///           -> Bước 4: Hỏi `ITechnicianDirectory.FindAsync` sang module `Identity` để xác minh Kỹ thuật viên chính
///              (và Kỹ thuật viên phụ nếu có) đúng Role `Technician`, đang `IsActive` và thuộc đúng phòng bảo trì.
///           -> Bước 5: Gọi `ticket.Assign(...)` trên Domain Aggregate + Ghi `TicketStatusHistory` trong cùng 1 Transaction.
/// </summary>
public sealed class AssignMaintenanceTicketHandler(
    ITicketAssignmentStore store,
    ITechnicianDirectory technicianDirectory,
    IEquipmentTicketContextReader equipmentReader)
{
    public async Task<TicketAssignmentOutcome> Handle(
        AssignMaintenanceTicketCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await store.GetForAssignmentAsync(command.TicketId, cancellationToken);
        if (ticket is null)
            return Fail(TicketAssignmentError.NotFound, "Ticket Not Found");

        // Lấy thông tin phòng bảo trì qua contract của Equipment. Maintenance không cần đọc
        // navigation/entity Assets để quyết định quyền, dù hai module hiện vẫn dùng chung DB.
        var equipment = await equipmentReader.GetAsync(ticket.EquipmentId, cancellationToken);

        // Giữ nguyên phạm vi phân quyền: Manager chỉ phân công thiết bị thuộc queue bảo trì của mình.
        var canAssign = command.ActorRole == UserRoles.Admin ||
            (command.ActorRole == UserRoles.Manager && command.ActorDepartmentId.HasValue &&
             equipment?.MaintenanceDepartmentId == command.ActorDepartmentId);
        if (!canAssign)
            return Fail(TicketAssignmentError.Forbidden, "Forbidden");

        if (equipment is null)
            return Fail(TicketAssignmentError.NotFound, "Equipment not found");
        if (!equipment.MaintenanceDepartmentId.HasValue)
            return Invalid("This equipment is self-managed and cannot be assigned to a technician");

        var targetDepartmentId = equipment.MaintenanceDepartmentId.Value;
        if (ticket.Status is TicketStatuses.Closed or TicketStatuses.Cancelled)
            return Invalid("Cannot assign a closed or cancelled ticket");
        if (!TicketWorkflowRules.IsAssignableStatus(ticket.Status))
            return Invalid("Only pending or assigned tickets can be assigned ");

        var technician = await technicianDirectory.FindAsync(command.AssignedTechnicianId, cancellationToken);
        if (technician is null)
            return Fail(TicketAssignmentError.NotFound, "Technician does not exists");
        var technicianError = ValidateTechnician(technician, targetDepartmentId, "Assigned");
        if (technicianError is not null)
            return technicianError;

        if (command.SupportTechnicianId.HasValue)
        {
            var supportTechnician = await technicianDirectory.FindAsync(
                command.SupportTechnicianId.Value, cancellationToken);
            if (supportTechnician is null)
                return Fail(TicketAssignmentError.NotFound, "Support technician does not exists");

            var supportError = ValidateTechnician(supportTechnician, targetDepartmentId, "Support");
            if (supportError is not null)
                return supportError;
            if (supportTechnician.Id == technician.Id)
                return Invalid("Support technician must be different from the main technician");
        }

        // Hành vi hiện tại không cho submit lại cùng technician chính, kể cả khi chỉ muốn
        // đổi technician hỗ trợ; giữ nguyên để đợt tách use case không âm thầm đổi API.
        if (ticket.AssignedTechnicianId == command.AssignedTechnicianId)
            return Invalid("Technician already assigned");

        var oldStatus = ticket.Status;
        ticket.Assign(command.AssignedTechnicianId, command.SupportTechnicianId);
        store.AddStatusHistory(new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatuses.Assigned,
            ChangedByUserId = command.ActorUserId,
            ChangedAt = DateTime.UtcNow,
            Note = command.Note
        });

        // Ranh giới transaction của use case này là Ticket + StatusHistory: cả hai đều được track
        // bởi Maintenance store và được ghi bằng cùng một SaveChanges. Equipment/Identity chỉ cung
        // cấp dữ liệu đọc để kiểm tra quyền; use case không sửa dữ liệu của hai module đó.
        await store.SaveChangesAsync(cancellationToken);
        var response = await store.GetResponseAsync(ticket.Id, cancellationToken);
        if (response is null)
            throw new InvalidOperationException($"Ticket {ticket.Id} was assigned but its response could not be loaded.");

        return TicketAssignmentOutcome.Success(response);
    }

    private static TicketAssignmentOutcome? ValidateTechnician(
        TechnicianRecord technician,
        int departmentId,
        string label)
    {
        if (technician.RoleName != UserRoles.Technician)
            return Invalid($"{label} user must be a technician");
        if (technician.DepartmentId != departmentId)
            return Invalid($"{label} technician must belong to the maintenance department for this equipment");
        if (!technician.IsActive)
            return Invalid($"{label} technician account is inactive");
        return null;
    }

    private static TicketAssignmentOutcome Invalid(string message) =>
        Fail(TicketAssignmentError.InvalidRequest, message);

    private static TicketAssignmentOutcome Fail(TicketAssignmentError error, string message) =>
        TicketAssignmentOutcome.Fail(error, message);
}
