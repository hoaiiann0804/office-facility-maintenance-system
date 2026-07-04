
using System.ComponentModel.DataAnnotations;
namespace InternalMaintenance.Api.DTOs.Auth;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}