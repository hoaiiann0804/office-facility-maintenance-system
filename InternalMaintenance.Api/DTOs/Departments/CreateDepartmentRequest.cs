
using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.DTOs.Departments;
public class CreateDepartmentRequest
{
    [Required(ErrorMessage = "Department name is required")]
    [StringLength(100, MinimumLength =2, ErrorMessage ="Department name must be between 2 and 100 characters")]
    public string Name {get;set;} = string.Empty;

    [Required(ErrorMessage ="Description must not exceed 500 characters")]
    public string? Description {get;set;}
}