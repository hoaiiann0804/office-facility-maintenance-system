using InternalMaintenance.Api.Common.Results;

namespace InternalMaintenance.Api.Modules.TicketAttachments.Storage;

public interface IAttachmentStorageService
{
    Task<ServiceResult<string>> CreateUploadUrlAsync(
        string storageKey,
        string contentType);

    Task<ServiceResult<string>> CreateDownloadUrlAsync(string storageKey);

    Task<ServiceResult<bool>> ObjectExistsAsync(string storageKey);

    Task<ServiceResult> DeleteObjectAsync(string storageKey);
}
