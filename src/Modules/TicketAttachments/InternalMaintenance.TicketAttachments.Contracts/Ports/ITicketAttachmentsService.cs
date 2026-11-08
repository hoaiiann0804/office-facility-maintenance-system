using InternalMaintenance.BuildingBlocks.Results;

namespace InternalMaintenance.TicketAttachments.Contracts;

/// <summary>
/// Cổng công khai của module để API host gọi use case mà không biết cách lưu EF hay dùng storage nào.
/// </summary>
public interface ITicketAttachmentsService
{
    Task<ServiceResult<PresignAttachmentResponse>> CreatePresignAsync(int ticketId, PresignAttachmentRequest request);
    Task<ServiceResult<TicketAttachmentResponse>> ConfirmAsync(int ticketId, ConfirmAttachmentRequest request);
    Task<ServiceResult<List<TicketAttachmentResponse>>> GetByTicketIdAsync(int ticketId);
    Task<ServiceResult<string>> GetDownloadUrlAsync(int ticketId, int attachmentId);
    Task<ServiceResult> DeleteAsync(int ticketId, int attachmentId);
}

/// <summary>Thông tin actor của request, tách implementation khỏi CurrentUserService thuộc API host.</summary>
public interface ITicketAttachmentActorContext
{
    int UserId { get; }
    string Role { get; }
    int? DepartmentId { get; }
}
