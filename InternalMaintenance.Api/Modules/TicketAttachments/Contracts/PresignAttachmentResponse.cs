namespace InternalMaintenance.Api.Modules.TicketAttachments.Contracts;

// Kết quả bước 1: URL upload, storage key và tên file đã sinh.
public class PresignAttachmentResponse
{
    public string UploadUrl { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
}
