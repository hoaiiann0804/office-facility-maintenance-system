namespace InternalMaintenance.Assets.Domain;

// Bảng Equipment là bảng lưu danh sách thiết bị công ty. 
// Mỗi thiết bị thuộc một phòng ban, có trạng thái hoạt động, ngày mua,mô tả và danh sách bảo trì liên quan
public class Equipment
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    // Phòng ban chịu trách nhiệm bảo trì thiết bị này (VD: Phòng IT, Phòng Hành Chính)
    public int? MaintenanceDepartmentId { get; set; }

    // Assets không giữ collection MaintenanceTicket để Domain thiết bị không phụ thuộc Domain bảo trì.
    // Khóa EquipmentId và FK vật lý vẫn do quan hệ persistence cấu hình ở CrossModuleRelationships.
    public string Status { get; set; } = "Active";
    public DateTime? PurchasedDate { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
};
