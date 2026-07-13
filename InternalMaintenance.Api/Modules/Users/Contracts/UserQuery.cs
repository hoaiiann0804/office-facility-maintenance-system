using System.ComponentModel.DataAnnotations;
using InternalMaintenance.Api.Common.Pagination;
namespace InternalMaintenance.Api.Modules.Users.Contracts;

public class UserQuery : PaginationQuery
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }

    public int? DepartmentId { get; set; }


    public bool? IsActive { get; set; }

}
