using InternalMaintenance.BuildingBlocks.Pagination;

namespace InternalMaintenance.Maintenance.Contracts;

// Bộ lọc này thuộc danh sách ticket Maintenance; request HTTP có thể dùng trực tiếp,
// trong khi các rule phân trang dùng lại BuildingBlocks mà không phụ thuộc vào API.
public class TicketQuery : PaginationQuery
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? EquipmentId { get; set; }
}
