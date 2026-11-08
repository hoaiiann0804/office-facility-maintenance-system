namespace InternalMaintenance.Maintenance.Contracts;

/// <summary>
/// Projection tối thiểu cho module khác kiểm tra quyền và xử lý file của một ticket.
/// Không trả entity MaintenanceTicket để module gọi không phụ thuộc Domain/EF của Maintenance.
/// </summary>
public sealed record MaintenanceTicketAccessContext(
    int TicketId,
    string TicketCode,
    string Status,
    int EquipmentId,
    int CreatedByUserId,
    int? AssignedTechnicianId,
    int? SupportTechnicianId);
