namespace InternalMaintenance.TicketAttachments.Contracts;

// Kết quả nghiệp vụ xóa metadata/object, không để contract phụ thuộc vào HTTP response type.
public class DeleteAttachmentResponse
{
    public string Message { get; set; } = string.Empty;
}
