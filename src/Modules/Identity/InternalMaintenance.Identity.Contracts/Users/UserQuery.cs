using System.ComponentModel.DataAnnotations;
using InternalMaintenance.BuildingBlocks.Pagination;
namespace InternalMaintenance.Identity.Contracts;

public class UserQuery : PaginationQuery
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }

    public int? DepartmentId { get; set; }


    public bool? IsActive { get; set; }

}
