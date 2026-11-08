
using System.ComponentModel.DataAnnotations;
namespace InternalMaintenance.Identity.Contracts;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
