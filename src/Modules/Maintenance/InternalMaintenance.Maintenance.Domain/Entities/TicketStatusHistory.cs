namespace InternalMaintenance.Maintenance.Domain;

public class TicketStatusHistory
{
    public int Id { get; set; }
    public int MaintenanceTicketId { get; set; }
    // Lịch sử trạng thái là dữ liệu nội bộ của Maintenance, gắn với ticket bằng FK và navigation cùng module.
    public MaintenanceTicket? MaintenanceTicket { get; set; }

    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    // Giữ ID tác nhân; tên được truy vấn qua Identity contract ở lúc dựng response.
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? Note { get; set; }
}
