
namespace InternalMaintenance.Api.DTOs.Auth;

public class AuthUserResponse
{
    
    public int Id { get; set;}

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public int RoleId {get;set;} 
    public string RoleName {get;set;}=string.Empty;
    public int? DepartmentId {get;set;}
    public string? DepartmentName {get;set;}

    public bool IsActive {get;set;}
    public bool MustChangePassword { get; set; }

}