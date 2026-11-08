namespace InternalMaintenance.TicketAttachments.Contracts;

// Request tạo URL upload tạm thời; file bytes không đi qua API host.
public class PresignAttachmentRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
