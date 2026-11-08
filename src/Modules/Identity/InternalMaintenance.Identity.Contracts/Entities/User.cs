namespace InternalMaintenance.Identity.Domain;

/// <summary>
/// Entity User biểu diễn tài khoản người dùng trong hệ thống.
/// [Lean Modular Monolith Refactoring]:
/// Di chuyển từ InternalMaintenance.Identity.Domain sang InternalMaintenance.Identity.Contracts
/// để Persistence có thể truy cập DbSet<User> và Configuration mà không gây circular dependency,
/// đồng thời giúp loại bỏ project Domain rỗng (giảm số lượng project từ 25 xuống dưới 10).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
