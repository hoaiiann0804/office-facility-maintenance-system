using InternalMaintenance.BuildingBlocks.Results;

namespace InternalMaintenance.TicketAttachments.Infrastructure;

public interface IAttachmentStorageService
{
    Task<ServiceResult<string>> CreateUploadUrlAsync(
        string storageKey,
        string contentType);

    Task<ServiceResult<string>> CreateDownloadUrlAsync(string storageKey);

    Task<ServiceResult<AttachmentObjectMetadata?>> GetObjectMetadataAsync(string storageKey);

    Task<ServiceResult> DeleteObjectAsync(string storageKey);
}

/// <summary>Metadata do storage xác nhận, không lấy từ giá trị do client khai báo.</summary>
public sealed record AttachmentObjectMetadata(long ContentLength);
