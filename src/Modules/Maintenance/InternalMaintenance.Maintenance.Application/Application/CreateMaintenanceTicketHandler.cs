using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Maintenance.Domain;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// [VERTICAL SLICE WRITE HANDLER: CREATE MAINTENANCE TICKET - TẠO PHIẾU BẢO TRÌ MỚI]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/tickets (MaintenanceTicketsController)
///           -> CreateMaintenanceTicketHandler.Handle(command, cancellationToken)
///           -> Bước 1: Kiểm tra hợp lệ Title &amp; Description.
///           -> Bước 2 (Giao tiếp liên module qua Contract):
///              Gọi `IEquipmentTicketContextReader.GetAsync(EquipmentId)` sang module `Equipment` để kiểm tra
///              thiết bị tồn tại, chưa thanh lý (`Retired`), chưa đang bảo trì (`UnderMaintenance`) và có phòng bảo trì phụ trách.
///           -> Bước 3 (Chống tạo trùng Ticket cho cùng 1 thiết bị):
///              Kiểm tra `ITicketCreationStore.HasEquipmentOccupyingTicketAsync` + chốt chặn cuối bằng Filtered Unique Index dưới SQL.
///           -> Bước 4 (Domain Aggregate Factory):
///              Gọi `MaintenanceTicket.Create(...)` để tự động tính hạn SLA (`DueAt`) theo `Priority` (`SlaPolicy`).
///           -> Bước 5: Lưu qua `ITicketCreationStore.TryAddAndSaveAsync` và nạp lại `MaintenanceTicketResponse`.
/// </summary>
public sealed class CreateMaintenanceTicketHandler(
    IEquipmentTicketContextReader equipmentReader,
    ITicketCreationStore store,
    ITicketCodeGenerator ticketCodeGenerator)
{
    public async Task<CreateMaintenanceTicketOutcome> Handle(
        CreateMaintenanceTicketCommand command,
        CancellationToken cancellationToken)
    {
        // Kiểm tra dữ liệu bắt buộc trước khi gọi module khác hoặc truy vấn DB, để request sai
        // được trả lỗi ngay và không tạo side effect ngoài ý muốn khi use case được gọi nội bộ.
        if (string.IsNullOrWhiteSpace(command.Title))
            return Invalid("Ticket title is required");
        if (string.IsNullOrWhiteSpace(command.Description))
            return Invalid("Ticket description is required");

        var equipment = await equipmentReader.GetAsync(command.EquipmentId, cancellationToken);
        if (equipment is null)
            return CreateMaintenanceTicketOutcome.Fail(CreateMaintenanceTicketError.EquipmentNotFound,
                "Equipment does not exist");

        if (equipment.Status == EquipmentStatuses.Retired)
            return Invalid("Cannot create ticket for retired equipment");
        if (equipment.Status == EquipmentStatuses.UnderMaintenance)
            return Invalid("Equipment is already under maintenance");
        if (!equipment.MaintenanceDepartmentId.HasValue)
            return Invalid("This equipment is self-managed and does not create maintenance tickets");

        // Kiểm tra trước giúp trả lỗi rõ ràng cho request thông thường. Tuy nhiên hai request
        // có thể cùng vượt qua bước đọc này, nên filtered unique index vẫn là chốt chống trùng cuối.
        if (await store.HasEquipmentOccupyingTicketAsync(command.EquipmentId, cancellationToken))
            return Duplicate();

        var priority = string.IsNullOrWhiteSpace(command.Priority)
            ? TicketPriorities.Medium
            : command.Priority.Trim();
        if (!TicketWorkflowRules.IsAllowedPriority(priority))
            return Invalid("Invalid ticket priority");

        var createdAt = DateTime.UtcNow;
        var ticket = MaintenanceTicket.Create(
            ticketCodeGenerator.GenerateTicketCode(),
            command.Title.Trim(),
            command.Description.Trim(),
            command.EquipmentId,
            command.CreatedByUserId,
            priority,
            createdAt);

        // Use case chỉ tạo dữ liệu thuộc Maintenance; Equipment được đọc để xác thực điều kiện
        // nhưng không bị thay đổi ở bước tạo. Unique filtered index là chốt đồng thời tại SQL,
        // còn SaveChanges bên store chỉ commit ticket mới.
        if (!await store.TryAddAndSaveAsync(ticket, cancellationToken))
            return CreateMaintenanceTicketOutcome.Fail(CreateMaintenanceTicketError.DuplicateRace,
                "This equipment already has an active maintenance ticket");

        var response = await store.GetResponseAsync(ticket.Id, cancellationToken);
        if (response is null)
            throw new InvalidOperationException($"Ticket {ticket.Id} was saved but its response could not be loaded.");

        return CreateMaintenanceTicketOutcome.Success(ticket.Id, response);
    }

    private static CreateMaintenanceTicketOutcome Invalid(string message) =>
        CreateMaintenanceTicketOutcome.Fail(CreateMaintenanceTicketError.InvalidRequest, message);

    private static CreateMaintenanceTicketOutcome Duplicate() =>
        CreateMaintenanceTicketOutcome.Fail(CreateMaintenanceTicketError.DuplicateActiveTicket,
            "This equipment already has an open maintenance ticket");
}
