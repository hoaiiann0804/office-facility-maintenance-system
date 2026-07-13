
using System.ComponentModel.DataAnnotations;
namespace InternalMaintenance.Api.Modules.Auth.Contracts;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
