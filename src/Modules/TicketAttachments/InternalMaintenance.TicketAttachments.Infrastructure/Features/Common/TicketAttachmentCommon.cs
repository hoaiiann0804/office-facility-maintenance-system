using System.IO;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.TicketAttachments.Domain;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.Common;

/// <summary>
/// [SHARED CONCERN TRONG NỘI BỘ MODULE TICKET ATTACHMENTS]
/// Chứa các quy tắc đặt tên khóa lưu trữ R2/S3 (`StorageKey`, `StoredFileName`), ánh xạ DTO
/// và kiểm tra quyền truy cập Ticket liên module thông qua `IMaintenanceTicketLookup` &amp; `IMaintenanceTicketAccessChecker`.
///
/// Giải quyết vấn đề khi tái cấu trúc:
/// - Trước đây toàn bộ các hàm helper này nằm trộn lẫn cuối file `TicketAttachmentsService.cs` (647 dòng).
/// - Tách về `Features/Common/TicketAttachmentCommon` giúp 5 Vertical Slices (`CreatePresign`, `ConfirmAttachment`,
///   `GetTicketAttachments`, `GetDownloadUrl`, `DeleteAttachment`) dùng chung quy ước đường dẫn
///   `tickets/{TicketCode}/attachments/attachment-{timestamp}-{guid}.{ext}` một cách nhất quán.
/// </summary>
internal static class TicketAttachmentCommon
{
    public static MaintenanceTicketActor GetCurrentActor(ITicketAttachmentActorContext actorContext) => new(
        actorContext.UserId,
        actorContext.Role,
        actorContext.DepartmentId);

    public static string BuildStoredFileName(string originalFileName, string contentType)
    {
        var extension = ResolveFileExtension(originalFileName, contentType);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8];

        return $"attachment-{stamp}-{random}{extension}";
    }

    public static string BuildStorageKey(string ticketCode, string storedFileName) =>
        BuildAttachmentPrefix(ticketCode) + storedFileName;

    public static string BuildAttachmentPrefix(string ticketCode) =>
        $"tickets/{ticketCode}/attachments/";

    public static string ResolveFileExtension(string originalFileName, string contentType)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.ToLowerInvariant();
        }

        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            "application/pdf" => ".pdf",
            _ => string.Empty
        };
    }

    public static TicketAttachmentResponse MapToResponse(TicketAttachment attachment, string uploaderName) => new()
    {
        Id = attachment.Id,
        MaintenanceTicketId = attachment.MaintenanceTicketId,
        UploadedByUserId = attachment.UploadedByUserId,
        UploadedByUserName = uploaderName,
        OriginalFileName = attachment.OriginalFileName,
        StoredFileName = attachment.StoredFileName,
        ContentType = attachment.ContentType,
        FileSize = attachment.FileSize,
        StorageKey = attachment.StorageKey,
        FileType = attachment.FileType,
        CreatedAt = attachment.CreatedAt,
        IsDeleted = attachment.IsDeleted
    };
}

