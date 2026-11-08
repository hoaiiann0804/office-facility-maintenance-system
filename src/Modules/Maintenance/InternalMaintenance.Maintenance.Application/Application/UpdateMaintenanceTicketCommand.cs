namespace InternalMaintenance.Maintenance.Application;

/// <summary>Dữ liệu cập nhật và actor được lấy từ JWT; quyền không thể bị giả bằng body.</summary>
public sealed record UpdateMaintenanceTicketCommand(
    int TicketId,
    string Title,
    string Description,
    string Priority,
    int ActorUserId,
    string ActorRole,
    int? ActorDepartmentId);
