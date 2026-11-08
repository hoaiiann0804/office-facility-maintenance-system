namespace InternalMaintenance.Identity.Contracts;

// Chỉ mang dữ liệu cần cho rule phân công, không đưa entity Identity sang module Maintenance.
public sealed record TechnicianRecord(int Id, string? RoleName, int? DepartmentId, bool IsActive);
