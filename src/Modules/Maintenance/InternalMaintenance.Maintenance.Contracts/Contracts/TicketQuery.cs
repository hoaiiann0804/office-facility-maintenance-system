using InternalMaintenance.BuildingBlocks.Pagination;

namespace InternalMaintenance.Maintenance.Contracts;

// Bộ lọc này thuộc danh sách ticket Maintenance; request HTTP có thể dùng trực tiếp,
// trong khi các rule phân trang dùng lại BuildingBlocks mà không phụ thuộc vào API.
public class TicketQuery : PaginationQuery
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? EquipmentId { get; set; }
    // [FIX F-01 / G-08 - TÌM KIẾM & LỌC SLA PHÍA SERVER]:
    // Bổ sung Keyword (tìm mã phiếu, tiêu đề, mô tả, tên/mã thiết bị, tên người tạo)
    // và SlaStatus (InSLA, NearBreach, Breached, MetSLA, Paused) để lọc trực tiếp dưới SQL Server
    // trên toàn bộ dữ liệu trước khi phân trang, thay vì chỉ lọc cục bộ 10 bản ghi ở Client.
    public string? Keyword { get; set; }
    public string? SlaStatus { get; set; }
}
