namespace InternalMaintenance.Maintenance.Domain;

public class TicketVendorLog
{
    public int Id { get; set; }

    public int MaintenanceTicketId { get; set; }

    // Vendor log thuộc vòng đời ticket; giữ quan hệ trong module Maintenance, còn Vendor chỉ được tham chiếu bằng ID.
    public MaintenanceTicket? MaintenanceTicket { get; set; }

    public int VendorId { get; set; }
    // Tên vendor do module Vendors cung cấp qua contract; log chỉ lưu khóa tham chiếu.

    // Lưu snapshot hai phòng ban khi gửi vendor để báo cáo lịch sử không đổi nếu thiết bị chuyển phòng sau này.
    // Nullable để các log cũ sau migration vẫn có thể dùng dữ liệu Equipment hiện tại làm phương án dự phòng.
    public int? DepartmentIdAtDispatch { get; set; }
    public int? MaintenanceDepartmentIdAtDispatch { get; set; }

    public DateTime DispatchedAt { get; set; }

    public DateTime EstimatedReturnDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    public int PausedMinutes { get; set; }

    public decimal? RepairCost { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
