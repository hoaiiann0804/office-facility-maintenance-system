using System.ComponentModel.DataAnnotations;
namespace InternalMaintenance.Identity.Contracts;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
