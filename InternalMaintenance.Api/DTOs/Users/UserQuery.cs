using System.ComponentModel.DataAnnotations;
namespace InternalMaintenance.Api.DTOs.Users;

public class UserQuery
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }

    public int? DepartmentId { get; set; }


    public bool? IsActive { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

}