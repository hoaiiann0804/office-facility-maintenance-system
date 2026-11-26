namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// Dữ liệu use case cần để xử lý một lần chuyển trạng thái.
/// Actor được dựng từ claims đã xác thực ở API; không lấy quyền từ body của client.
/// Actor được truyền dưới dạng scalar snapshot thay vì entity User để Maintenance chỉ biết
/// đúng dữ liệu cần kiểm tra quyền và không phụ thuộc model Identity.
/// </summary>
public sealed record ChangeTicketStatusCommand(
    int TicketId,
    string? NewStatus,
    string? ResolutionNote,
    string? CancellationReason,
    string? Note,
    int? VendorId,
    DateTime? VendorEstimatedReturnDate,
    string? VendorNote,
    int ActorUserId,
    string ActorRole,
    int? ActorDepartmentId,
    // [FIX G-01]: Bổ sung chi phí sửa chữa thực tế của đối tác (RepairCost) khi nhận lại thiết bị từ Vendor.
    decimal? VendorRepairCost = null);
