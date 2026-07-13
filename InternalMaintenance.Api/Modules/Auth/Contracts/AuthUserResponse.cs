
namespace InternalMaintenance.Api.Modules.Auth.Contracts;

public class AuthUserResponse
{
    
    public int Id { get; set;}

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string RoleName {get;set;}=string.Empty;
    public int? DepartmentId {get;set;}
    public string? DepartmentName {get;set;}

    public bool IsActive {get;set;}
    public bool MustChangePassword { get; set; }

}
