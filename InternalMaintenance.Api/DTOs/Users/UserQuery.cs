using System.ComponentModel.DataAnnotations;
using InternalMaintenance.Api.DTOs.Common;
namespace InternalMaintenance.Api.DTOs.Users;

public class UserQuery : PaginationQuery
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }

    public int? DepartmentId { get; set; }


    public bool? IsActive { get; set; }

}