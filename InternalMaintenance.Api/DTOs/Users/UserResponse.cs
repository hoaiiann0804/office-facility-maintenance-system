
namespace InternalMaintenance.Api.DTOs.Users;
public class UserResponse
{
    public int Id {get;set;}

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName {get;set;} = string.Empty;

    public int? DepartmentId {get;set;}

    public string? DepartmentName {get;set;}

    public bool IsActive {get;set;}

    public bool MustChangePassword {get;set;}

    public DateTime? LastLoginAt {get;set;}

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    
}