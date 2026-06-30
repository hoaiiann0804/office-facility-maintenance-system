using System.ComponentModel.DataAnnotations;
namespace InternalMaintenance.Api.DTOs.Users;
public class ResetUserPasswordRequest
{
    [Required]
    [MinLength(8)]
    public string TemporaryPassword { get; set; } = string.Empty;
}