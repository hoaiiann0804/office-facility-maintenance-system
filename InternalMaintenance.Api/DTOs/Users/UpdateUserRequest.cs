using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.DTOs.Users;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Full name must be between 2 and 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [Range(1, int.MaxValue,
        ErrorMessage = "RoleId must be greater than 0")]
    public int RoleId { get; set; }

    public int? DepartmentId { get; set; }
}