namespace InternalMaintenance.Api.Models;

public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    //Role?: thông tin chi tiết của role đó 
    // ? của Role có thể tạm thời là null nếu bạn chưa load dữ liệu từ database.
    // Nếu query (await dbContext.Users.Include(u=>u.Role).FirstOrDefaultAsync();) thì user.role sẽ có dữ liệu 
    public Role? Role { get; set; }

    // Phòng ban mà người dùng thuộc về
    //Lưu ý: Vì không phải user nào cũng bắt buộc phải thuộc phòng ban 
    //Ví dụ Admin hệ thống có thể không cần DepartmentId
    public int? DepartmentId { get; set; }

    //Thôg tin chi tiết của phòng ban. phòng ban giúp hệ thống biết trong nghiệp vụ: 
    //User này thuộc phòng ban nào
    //Thiết bị này thuộc phòng ban nào 
    //Manager có thể xem workload (khối lượng công việc) theo phòng ban không?
    public Department? Department { get; set; }

    public bool IsActive { get; set; }

    // User(Admin/Manager/Technician/Staff) sau khi đăng nhập lần đầu, Buộc phải thay đổi mật khẩu
    public bool MustChangePassword { get; set; } = true;

    // User đăng nhập lần cuối lúc nào 
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    //User mới tạo chưa có thể chưa từng được cập nhật nên dùng null (?)
    public DateTime? UpdatedAt { get; set; }

    //Chúng không phải column/field thật trong bảng Users.
    //Chúng là navigation properties của EF Core, dùng để mô tả quan hệ giữa các bảng trong code C#.

    //Danh sách ticket mà user này đã tạo(1-N)
    public ICollection<MaintenanceTicket> CreatedTickets { get; set; } = new List<MaintenanceTicket>();
    //Danh sách ticket mà technician này được giao
    public ICollection<MaintenanceTicket> AssignedTickets { get; set; } = new List<MaintenanceTicket>();
    //Danh sách lần user này thay đổi trạng thái ticket
    public ICollection<TicketStatusHistory> TicketStatusHistories { get; set; } = new List<TicketStatusHistory>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
