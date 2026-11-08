namespace InternalMaintenance.Maintenance.Contracts;

/// <summary>Thông tin actor lấy từ claims đã xác thực, truyền vào các truy vấn Maintenance.</summary>
public sealed record MaintenanceTicketActor(int UserId, string Role, int? DepartmentId);
