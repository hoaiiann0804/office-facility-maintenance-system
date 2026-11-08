using InternalMaintenance.Maintenance.Domain;

namespace InternalMaintenance.Persistence;

/// <summary>
/// Bản ghi bền vững cho một email SLA cần gửi. Đây là chi tiết persistence, nên nằm ngoài
/// Maintenance Domain; cùng DbContext giúp ghi notification và trạng thái ticket trong một transaction.
/// </summary>
public sealed class SlaNotificationOutboxMessage
{
    public long Id { get; set; }
    public int MaintenanceTicketId { get; set; }
    public DateTime DueAt { get; set; }
    public SlaNotificationType NotificationType { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public Guid? LeaseId { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? LastError { get; set; }
}
