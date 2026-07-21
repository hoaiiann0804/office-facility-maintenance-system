using InternalMaintenance.Api.Constants;

namespace InternalMaintenance.Api.Modules.TicketAttachments;

public static class TicketAttachmentRules
{
    // Nơi tập trung các rule nghiệp vụ của attachment: dung lượng, loại file và trạng thái ticket.
    public const long MaxUploadFileSizeBytes = 100L * 1024 * 1024;

    public static readonly string[] AllowedImageContentTypes =
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    public static readonly string[] AllowedVideoContentTypes =
    {
        "video/mp4",
        "video/webm"
    };

    public static readonly string[] AllowedDocumentContentTypes =
    {
        "application/pdf"
    };

    public static readonly string[] AllowedTicketStatuses =
    {
        TicketStatuses.Pending,
        TicketStatuses.Assigned,
        TicketStatuses.InProgress,
        TicketStatuses.Resolved
    };

    public static bool IsUploadAllowedStatus(string status)
    {
        return AllowedTicketStatuses.Contains(status);
    }

    public static bool IsAllowedContentType(string contentType)
    {
        return AllowedImageContentTypes.Contains(contentType)
            || AllowedVideoContentTypes.Contains(contentType)
            || AllowedDocumentContentTypes.Contains(contentType);
    }

    public static bool IsAllowedFileSize(long fileSize)
    {
        // Giới hạn dung lượng upload và không cho phép file rỗng.
        return fileSize > 0 && fileSize <= MaxUploadFileSizeBytes;
    }

    public static bool IsImage(string contentType)
    {
        return AllowedImageContentTypes.Contains(contentType);
    }

    public static bool IsVideo(string contentType)
    {
        return AllowedVideoContentTypes.Contains(contentType);
    }

    public static string ResolveFileType(string contentType)
    {
        // FileType là nhãn nghiệp vụ đơn giản để UI hiển thị.
        if (IsImage(contentType))
        {
            return "Image";
        }

        if (IsVideo(contentType))
        {
            return "Video";
        }

        return "Document";
    }
}
