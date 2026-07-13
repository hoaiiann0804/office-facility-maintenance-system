
using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.Modules.Auth.Contracts;

public class LoginRequest
{
    [Required(ErrorMessage ="Email is required")]
    [EmailAddress(ErrorMessage ="Email format is invalid")]
    public string Email {get;set;} = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

}
