using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.DTOs.Users;

public class CreateUserRequest
{
    [Required(ErrorMessage ="Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage ="Full name must be between 2 and 100 characters")]
    public string FullName {get;set;} = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage ="Email format is invalid")]
    public string Email {get;set;}= string.Empty;

    [Required(ErrorMessage ="Tempory password is required")]
    [MinLength(8, ErrorMessage ="Temporary password must be at least 8 characters")]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage ="RoleId must be greater than 0")]
    public int RoleId {get;set;}
    public int? DepartmentId { get; set; }
}
