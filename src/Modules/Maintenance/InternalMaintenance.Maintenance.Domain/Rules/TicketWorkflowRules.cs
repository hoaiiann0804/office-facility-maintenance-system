namespace InternalMaintenance.Maintenance.Domain;

public static class TicketWorkflowRules
{
    public static bool IsAllowedPriority(string priority) =>
        new[] { TicketPriorities.Low, TicketPriorities.Medium, TicketPriorities.High, TicketPriorities.Critical }
            .Contains(priority);

    // Ticket còn chiếm thiết bị tới khi đóng/hủy; dùng cả API lẫn filtered index.
    public static readonly string[] EquipmentOccupyingStatuses =
    [
        TicketStatuses.Pending,
        TicketStatuses.Assigned,
        TicketStatuses.InProgress,
        TicketStatuses.Resolved,
        TicketStatuses.WaitingForVendor
    ];

    // Chỉ các status này có đồng hồ SLA đang chạy.
    public static readonly string[] SlaTrackedStatuses =
    [
        TicketStatuses.Pending,
        TicketStatuses.Assigned,
        TicketStatuses.InProgress
    ];

    public static bool IsOpenTicketStatus(string status) =>
        EquipmentOccupyingStatuses.Contains(status);

    public static bool IsAssignableStatus(string status) =>
        status is TicketStatuses.Pending or TicketStatuses.Assigned;

    public static bool CanTransition(string currentStatus, string newStatus) =>
        (currentStatus == TicketStatuses.Pending && newStatus == TicketStatuses.Cancelled) ||
        (currentStatus == TicketStatuses.Assigned && newStatus == TicketStatuses.Cancelled) ||
        (currentStatus == TicketStatuses.Assigned && newStatus == TicketStatuses.InProgress) ||
        (currentStatus == TicketStatuses.Assigned && newStatus == TicketStatuses.WaitingForVendor) ||
        (currentStatus == TicketStatuses.InProgress && newStatus == TicketStatuses.Resolved) ||
        (currentStatus == TicketStatuses.InProgress && newStatus == TicketStatuses.WaitingForVendor) ||
        (currentStatus == TicketStatuses.WaitingForVendor && newStatus == TicketStatuses.InProgress) ||
        (currentStatus == TicketStatuses.Resolved && newStatus == TicketStatuses.Closed);
}
