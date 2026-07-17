namespace InternalMaintenance.Api.Models;

public class TicketAttachment
{

    public int Id { get; set; }

    public int MaintenanceTicketId { get; set; }

    // File đính kèm luôn gắn với một ticket cụ thể 
    // Lúc tạo object thì property này có thể chưa giá trị
    public MaintenanceTicket MaintenanceTicket { get; set; } = null!;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public string OriginalFileName { get; set; } = string.Empty;

    // Tên lưu thật trong storage
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StorageProvider { get; set; } = "R2";
    public string StorageKey { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // Image, Video, Document
    public string? FileHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }



}
