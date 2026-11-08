namespace InternalMaintenance.Assets.Contracts;

// Maintenance nhận đúng phần dữ liệu Equipment cần để áp dụng rule, không nhận entity Assets/EF.
public sealed record EquipmentTicketContext(
    string Status,
    int DepartmentId,
    int? MaintenanceDepartmentId,
    string Code = "",
    string Name = "");
