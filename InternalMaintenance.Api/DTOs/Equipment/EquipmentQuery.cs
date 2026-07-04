
using InternalMaintenance.Api.DTOs.Common;
namespace InternalMaintenance.Api.DTOs.Equipment;

public class EquipmentQuery : PaginationQuery
{
    public string? Keyword { get; set; }
    public int? DepartmentId { get; set; }

    public string? Status { get; set; }
}