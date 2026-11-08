namespace InternalMaintenance.TicketAttachments.Domain;

/// <summary>
/// Metadata của tệp đính kèm thuộc module TicketAttachments; nội dung tệp nằm ở object storage (Cloudflare R2 / S3).
/// [Lean Modular Monolith Refactoring]:
/// Di chuyển từ InternalMaintenance.TicketAttachments.Domain sang InternalMaintenance.TicketAttachments.Contracts
/// để Persistence có thể truy cập DbSet<TicketAttachment> mà không tạo circular dependency,
/// đồng thời giúp loại bỏ project Domain rỗng (giảm tải project tree).
/// </summary>
public class TicketAttachment
{
    public int Id { get; set; }
    public int MaintenanceTicketId { get; set; }
    // Chỉ giữ ID ticket; quyền và thông tin ticket được hỏi qua contract của Maintenance.
    public int UploadedByUserId { get; set; }
    // Chỉ giữ ID người tải; tên người dùng được hỏi qua contract của Identity.
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StorageProvider { get; set; } = "R2";
    public string StorageKey { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string? FileHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Null nghĩa là object chưa xóa xong; worker sẽ thử lại các bản ghi soft-delete này.
    public DateTime? StorageDeletedAt { get; set; }

    // Lease giúp nhiều instance không xóa cùng object; instance khác có thể tiếp tục khi lease hết hạn.
    public Guid? StorageDeleteLeaseId { get; set; }
    public DateTime? StorageDeleteLeaseUntil { get; set; }
}
