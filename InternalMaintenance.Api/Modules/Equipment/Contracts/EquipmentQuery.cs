
using InternalMaintenance.Api.Common.Pagination;
namespace InternalMaintenance.Api.Modules.Equipment.Contracts;

public class EquipmentQuery : PaginationQuery
{
    public string? Keyword { get; set; }
    public int? DepartmentId { get; set; }

    public string? Status { get; set; }
}
