namespace InternalMaintenance.TicketAttachments.Contracts;

// Request xác nhận metadata sau khi client tải file lên object storage thành công.
public class ConfirmAttachmentRequest
{
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string? FileHash { get; set; }
}
