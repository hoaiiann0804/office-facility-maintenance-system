using InternalMaintenance.Api.Common.Results;
using InternalMaintenance.Api.Modules.TicketAttachments.Contracts;

namespace InternalMaintenance.Api.Modules.TicketAttachments;

public interface ITicketAttachmentsService
{
    Task<ServiceResult<PresignAttachmentResponse>> CreatePresignAsync(int ticketId, PresignAttachmentRequest request);

    Task<ServiceResult<TicketAttachmentResponse>> ConfirmAsync(int ticketId, ConfirmAttachmentRequest request);
    Task<ServiceResult<List<TicketAttachmentResponse>>> GetByTicketIdAsync(int ticketId);
    Task<ServiceResult<string>> GetDownloadUrlAsync(int ticketId, int attachmentId);
    Task<ServiceResult> DeleteAsync(int ticketId, int attachmentId);
}
