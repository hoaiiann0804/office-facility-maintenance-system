namespace InternalMaintenance.Api.Modules.Tickets.Contracts;

/// <summary>Chỉ chứa ID cần cho policy; không đưa entity của module khác vào kiểm tra quyền.</summary>
public sealed record TicketAccessSubject(
    int EquipmentId,
    int CreatedByUserId,
    int? AssignedTechnicianId,
    int? SupportTechnicianId);
