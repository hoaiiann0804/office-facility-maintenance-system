using InternalMaintenance.Maintenance.Domain;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>Thông tin tối thiểu để quản trị viên biết email SLA nào cần được xử lý lại.</summary>
public sealed record SlaOutboxFailure(
    long OutboxId,
    int MaintenanceTicketId,
    string TicketCode,
    SlaNotificationType NotificationType,
    DateTime QueuedAt,
    DateTime FailedAt,
    int AttemptCount,
    string? Error);
