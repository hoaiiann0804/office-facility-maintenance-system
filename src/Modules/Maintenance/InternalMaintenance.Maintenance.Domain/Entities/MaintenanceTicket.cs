namespace InternalMaintenance.Maintenance.Domain;

// MaintenanceTicket = phieu bao loi / yeu cau bao tri.
// Staff tao ticket, sau do Admin/Manager phan cong technician xu ly.
public class MaintenanceTicket
{
    public int Id { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public int EquipmentId { get; set; }

    // Nguoi tao ticket.
    public int CreatedByUserId { get; set; }

    // Technician chinh phu trach ticket.
    public int? AssignedTechnicianId { get; set; }

    // Technician ho tro them neu can.
    public int? SupportTechnicianId { get; set; }

    public string Priority { get; private set; } = TicketPriorities.Medium;
    public string Status { get; private set; } = TicketStatuses.Pending;
    public string? ResolutionNote { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // Thông tin cấu hình SLA 
    // Hạn chót cần xử lý xong ticket
    public DateTime? DueAt { get; private set; }
    // Trạng thái SLA (ví dụ: InSLA, MetSLA, MissedSLA, Breached)
    public string SlaStatus { get; private set; } = SlaPolicy.InSLA;
    // Thời điểm cảnh báo sắp vi phạm đã được xếp hàng trong outbox (không khẳng định email đã tới inbox).
    public DateTime? NearBreachNotifiedAt { get; private set; }
    // Thời điểm cảnh báo vi phạm đã được xếp hàng trong outbox.
    public DateTime? BreachedNotifiedAt { get; private set; }
    // Thời điểm cảnh báo leo thang đã được xếp hàng trong outbox.
    public DateTime? EscalatedNotifiedAt { get; private set; }

    // Thông tin tạm dừng SLA khi gửi Vendor
    public DateTime? SlaPausedAt { get; private set; }
    public int TotalSlaPausedMinutes { get; private set; }

    public ICollection<TicketStatusHistory> StatusHistories { get; set; } = new List<TicketStatusHistory>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    // Không giữ collection attachment: module Attachments truy vấn theo MaintenanceTicketId
    // qua DbSet, còn FK một chiều được cấu hình tại persistence để Domain không phụ thuộc module đó.
    public ICollection<TicketVendorLog> VendorLogs { get; set; } = new List<TicketVendorLog>();

    // Tạo ticket qua factory để DueAt và trạng thái SLA khởi tạo đồng bộ với priority.
    public static MaintenanceTicket Create(
        string ticketCode,
        string title,
        string description,
        int equipmentId,
        int createdByUserId,
        string priority,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(ticketCode))
            throw new ArgumentException("Ticket code is required.", nameof(ticketCode));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Ticket title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Ticket description is required.", nameof(description));
        if (!TicketWorkflowRules.IsAllowedPriority(priority))
            throw new ArgumentException("Invalid ticket priority.", nameof(priority));

        return new MaintenanceTicket
        {
            TicketCode = ticketCode.Trim(),
            Title = title.Trim(),
            Description = description.Trim(),
            EquipmentId = equipmentId,
            CreatedByUserId = createdByUserId,
            Priority = priority,
            Status = TicketStatuses.Pending,
            CreatedAt = createdAt,
            DueAt = SlaPolicy.CalculateDueAt(createdAt, priority),
            SlaStatus = SlaPolicy.InSLA
        };
    }

    public void Assign(int assignedTechnicianId, int? supportTechnicianId)
    {
        if (!TicketWorkflowRules.IsAssignableStatus(Status))
            throw new InvalidOperationException("Ticket is not in an assignable status.");

        AssignedTechnicianId = assignedTechnicianId;
        SupportTechnicianId = supportTechnicianId;
        Status = TicketStatuses.Assigned;
    }

    public void ChangePriority(string priority, DateTime now)
    {
        if (!TicketWorkflowRules.IsAllowedPriority(priority))
            throw new ArgumentException("Invalid ticket priority.", nameof(priority));
        if (Status is TicketStatuses.Resolved or TicketStatuses.Closed or TicketStatuses.Cancelled)
            throw new InvalidOperationException("A finalized ticket cannot change priority.");

        Priority = priority;
        DueAt = SlaPolicy.CalculateDueAt(CreatedAt, priority)
            .AddMinutes(TotalSlaPausedMinutes);
        NearBreachNotifiedAt = null;
        BreachedNotifiedAt = null;
        EscalatedNotifiedAt = null;

        // Trong WaitingForVendor, thời gian pause hiện tại chưa được cộng vào tổng.
        // Khi thiết bị quay lại, ReturnFromVendor cộng đúng khoảng pause một lần.
        SlaStatus = Status == TicketStatuses.WaitingForVendor
            ? SlaPolicy.Paused
            : SlaPolicy.GetActiveStatus(DueAt.Value, now);
    }

    /// <summary>
    /// Cập nhật nội dung qua aggregate để mọi caller cùng tuân theo quy tắc: ticket đã kết thúc
    /// không được sửa và tiêu đề/mô tả không thể bị ghi thành chuỗi rỗng.
    /// </summary>
    public void UpdateDetails(string title, string description)
    {
        if (Status is TicketStatuses.Resolved or TicketStatuses.Closed or TicketStatuses.Cancelled)
            throw new InvalidOperationException("A finalized ticket cannot be edited.");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Ticket title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Ticket description is required.", nameof(description));

        Title = title.Trim();
        Description = description.Trim();
    }

    public void SendToVendor(DateTime dispatchedAt)
    {
        if (Status is not (TicketStatuses.Assigned or TicketStatuses.InProgress))
            throw new InvalidOperationException("Only an assigned or in-progress ticket can be sent to a vendor.");

        Status = TicketStatuses.WaitingForVendor;
        SlaPausedAt = dispatchedAt;
        SlaStatus = SlaPolicy.Paused;
    }

    public void ReturnFromVendor(DateTime actualReturnAt, int pausedMinutes)
    {
        if (Status != TicketStatuses.WaitingForVendor)
            throw new InvalidOperationException("Ticket is not waiting for a vendor.");
        if (pausedMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(pausedMinutes));

        TotalSlaPausedMinutes += pausedMinutes;
        if (DueAt.HasValue)
            DueAt = DueAt.Value.AddMinutes(pausedMinutes);
        SlaPausedAt = null;
        Status = TicketStatuses.InProgress;
        SlaStatus = DueAt.HasValue
            ? SlaPolicy.GetActiveStatus(DueAt.Value, actualReturnAt)
            : SlaPolicy.InSLA;
    }

    public void Start(DateTime startedAt)
    {
        if (Status != TicketStatuses.Assigned)
            throw new InvalidOperationException("Only an assigned ticket can be started.");
        Status = TicketStatuses.InProgress;
        SlaStatus = DueAt.HasValue
            ? SlaPolicy.GetActiveStatus(DueAt.Value, startedAt)
            : SlaPolicy.InSLA;
    }

    public void Resolve(string resolutionNote, DateTime resolvedAt)
    {
        if (Status != TicketStatuses.InProgress)
            throw new InvalidOperationException("Only an in-progress ticket can be resolved.");
        if (string.IsNullOrWhiteSpace(resolutionNote))
            throw new ArgumentException("Resolution note is required.", nameof(resolutionNote));

        Status = TicketStatuses.Resolved;
        ResolutionNote = resolutionNote.Trim();
        ResolvedAt = resolvedAt;
        if (DueAt.HasValue)
            SlaStatus = resolvedAt <= DueAt.Value ? SlaPolicy.MetSLA : SlaPolicy.MissedSLA;
    }

    public void Close(DateTime closedAt)
    {
        if (Status != TicketStatuses.Resolved)
            throw new InvalidOperationException("Only a resolved ticket can be closed.");
        Status = TicketStatuses.Closed;
        ClosedAt = closedAt;
    }

    public void Cancel(string reason)
    {
        if (Status is not (TicketStatuses.Pending or TicketStatuses.Assigned or TicketStatuses.InProgress))
            throw new InvalidOperationException("Ticket cannot be cancelled from its current status.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        Status = TicketStatuses.Cancelled;
        CancellationReason = reason.Trim();
    }

    // Worker hỏi aggregate những thông báo nào đến hạn. Nhờ vậy Resolved và
    // WaitingForVendor không thể bị xử lý như SLA đang chạy ở một nơi khác.
    public IReadOnlyList<SlaNotificationType> GetDueSlaNotifications(DateTime now)
    {
        if (!TicketWorkflowRules.SlaTrackedStatuses.Contains(Status) || !DueAt.HasValue)
            return Array.Empty<SlaNotificationType>();

        var dueAt = DueAt.Value;
        var notifications = new List<SlaNotificationType>();
        if (now > dueAt)
        {
            if (BreachedNotifiedAt is null)
                notifications.Add(SlaNotificationType.Breached);
            if (now > dueAt.AddHours(2) && EscalatedNotifiedAt is null)
                notifications.Add(SlaNotificationType.Escalated);
        }
        else if (dueAt - now <= TimeSpan.FromHours(1) && NearBreachNotifiedAt is null)
        {
            notifications.Add(SlaNotificationType.NearBreach);
        }

        return notifications;
    }

    // Đồng bộ snapshot trong bộ nhớ sau khi worker đã claim thành công trong SQL.
    public void MarkSlaNotificationClaimed(SlaNotificationType notification, DateTime notifiedAt)
    {
        switch (notification)
        {
            case SlaNotificationType.NearBreach:
                SlaStatus = SlaPolicy.NearBreach;
                NearBreachNotifiedAt = notifiedAt;
                break;
            case SlaNotificationType.Breached:
                SlaStatus = SlaPolicy.Breached;
                BreachedNotifiedAt = notifiedAt;
                break;
            case SlaNotificationType.Escalated:
                EscalatedNotifiedAt = notifiedAt;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(notification));
        }
    }
}
