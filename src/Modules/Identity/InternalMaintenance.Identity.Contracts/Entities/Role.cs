namespace InternalMaintenance.Identity.Domain;

/// <summary>
/// Entity Role biểu diễn vai trò của người dùng trong hệ thống (Admin, Manager, Technician, Requester).
/// [Lean Modular Monolith Refactoring]:
/// Di chuyển từ InternalMaintenance.Identity.Domain sang InternalMaintenance.Identity.Contracts.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<User> Users { get; set; } = new List<User>();
}
