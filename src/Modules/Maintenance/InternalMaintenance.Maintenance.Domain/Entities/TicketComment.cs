namespace InternalMaintenance.Maintenance.Domain;

public class TicketComment
{
    public int Id { get; set; }
    public int MaintenanceTicketId { get; set; }
    // Comment thuộc cùng module Maintenance với ticket; navigation nội bộ giữ phép cascade/query của aggregate.
    public MaintenanceTicket? MaintenanceTicket { get; set; }
    public int UserId { get; set; }
    // Lưu UserId làm tham chiếu ổn định; tên hiển thị được hỏi qua Identity contract,
    // để entity bình luận không phụ thuộc trực tiếp vào entity User của module Identity.
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

}
