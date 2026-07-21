namespace InternalMaintenance.Api.Modules.TicketAttachments.Contracts;

// Metadata chỉ để trả về cho client; không chứa file bytes.
public class TicketAttachmentResponse
{
    public int Id { get; set; }
    public int MaintenanceTicketId { get; set; }
    public int UploadedByUserId { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
