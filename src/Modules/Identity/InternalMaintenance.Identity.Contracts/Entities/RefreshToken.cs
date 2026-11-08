namespace InternalMaintenance.Identity.Domain;

/// <summary>
/// Entity RefreshToken quản lý phiên xác thực JWT dài hạn của người dùng.
/// [Lean Modular Monolith Refactoring]:
/// Di chuyển từ InternalMaintenance.Identity.Domain sang InternalMaintenance.Identity.Contracts.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public User User { get; set; } = null!;
}
