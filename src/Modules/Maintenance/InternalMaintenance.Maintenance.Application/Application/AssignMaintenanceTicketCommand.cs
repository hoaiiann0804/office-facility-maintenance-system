namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// Dữ liệu phân công; thông tin actor lấy từ JWT, không nhận từ body.
/// Chỉ truyền ID/role/department cần cho use case thay vì entity User của Identity,
/// để Maintenance không phụ thuộc model hoặc vòng đời persistence của module đó.
/// </summary>
public sealed record AssignMaintenanceTicketCommand(
    int TicketId,
    int AssignedTechnicianId,
    int? SupportTechnicianId,
    string? Note,
    int ActorUserId,
    string ActorRole,
    int? ActorDepartmentId);
