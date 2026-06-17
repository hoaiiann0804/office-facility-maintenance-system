
namespace InternalMaintenance.Api.DTOs.Auth;
public class LoginResponse
{
    public string AccessToken {get;set;}= string.Empty;
    public string TokenType {get;set;} ="Bearer";

    public int ExpiresInMinutes {get;set;}

    public bool MustChangePassword { get; set; }
    
    public AuthUserResponse User {get;set;} = new();
}