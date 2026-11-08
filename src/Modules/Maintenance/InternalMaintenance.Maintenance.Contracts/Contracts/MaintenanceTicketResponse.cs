namespace InternalMaintenance.Maintenance.Contracts;

// Response thuộc module Maintenance vì mô tả ticket và các dữ liệu nghiệp vụ đi kèm.
// Đặt ở assembly riêng giúp Application và adapter persistence không phải tham chiếu ngược API host.
public class MaintenanceTicketResponse
{
    public int Id { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public int EquipmentDepartmentId { get; set; }
    public int? EquipmentMaintenanceDepartmentId { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public int? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public int? SupportTechnicianId { get; set; }
    public string? SupportTechnicianName { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public string? SlaStatus { get; set; }
    public DateTime? SlaPausedAt { get; set; }
    public int TotalSlaPausedMinutes { get; set; }
    public List<TicketVendorLogResponse> VendorLogs { get; set; } = new();
}
