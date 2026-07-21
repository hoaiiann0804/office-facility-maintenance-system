namespace InternalMaintenance.Api.Modules.TicketAttachments.Contracts;

// Input cho bước 2: client xác nhận sau khi file đã upload xong lên storage.
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
