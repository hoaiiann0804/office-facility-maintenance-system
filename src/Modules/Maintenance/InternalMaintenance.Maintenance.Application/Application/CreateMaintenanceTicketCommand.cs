namespace InternalMaintenance.Maintenance.Application;

/// <summary>Thông tin tạo ticket; người tạo luôn lấy từ JWT đã xác thực, không tin ID trong request.</summary>
public sealed record CreateMaintenanceTicketCommand(
    string Title,
    string Description,
    int EquipmentId,
    string? Priority,
    int CreatedByUserId);
