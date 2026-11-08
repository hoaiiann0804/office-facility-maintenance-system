
using InternalMaintenance.BuildingBlocks.Pagination;
namespace InternalMaintenance.Assets.Contracts;

public class EquipmentQuery : PaginationQuery
{
    public string? Keyword { get; set; }
    public int? DepartmentId { get; set; }

    public string? Status { get; set; }
}
