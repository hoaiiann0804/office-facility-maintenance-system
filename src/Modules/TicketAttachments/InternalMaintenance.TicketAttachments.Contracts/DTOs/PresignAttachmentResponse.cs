namespace InternalMaintenance.TicketAttachments.Contracts;

// Trả URL có hạn dùng và storage key để client upload trực tiếp lên object storage.
public class PresignAttachmentResponse
{
    public string UploadUrl { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
}
