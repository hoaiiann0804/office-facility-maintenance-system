namespace InternalMaintenance.TicketAttachments.Domain;

/// <summary>
/// Ghi nhận trước một lần upload để worker còn biết StorageKey cần dọn nếu file đã lên storage
/// nhưng client không xác nhận được attachment trong SQL.
/// [Lean Modular Monolith Refactoring]:
/// Di chuyển từ InternalMaintenance.TicketAttachments.Domain sang InternalMaintenance.TicketAttachments.Contracts.
/// </summary>
public sealed class TicketAttachmentUploadIntent
{
    public int Id { get; set; }
    public int MaintenanceTicketId { get; set; }
    public int RequestedByUserId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UploadUrlExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? StorageDeletedAt { get; set; }
    public Guid? CleanupLeaseId { get; set; }
    public DateTime? CleanupLeaseUntil { get; set; }
}
