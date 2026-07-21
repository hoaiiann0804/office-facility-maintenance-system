namespace InternalMaintenance.Api.Modules.TicketAttachments.Contracts;

// Input cho bước 1: client xin URL upload tạm thời.
public class PresignAttachmentRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
