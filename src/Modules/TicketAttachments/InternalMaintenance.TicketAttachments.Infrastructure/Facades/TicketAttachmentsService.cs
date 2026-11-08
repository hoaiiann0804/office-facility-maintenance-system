using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Persistence;
using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.ConfirmAttachment;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.CreatePresign;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.DeleteAttachment;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.GetDownloadUrl;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.GetTicketAttachments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InternalMaintenance.TicketAttachments.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BỘ ĐIỀU PHỐI MỎNG CHO MODULE TICKET ATTACHMENTS]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    TicketAttachmentsController -> ITicketAttachmentsService (TicketAttachmentsService)
///      ├── CreatePresignAsync(...)   ---> CreatePresignAttachmentHandler.HandleAsync(...)
///      ├── ConfirmAsync(...)         ---> ConfirmAttachmentHandler.HandleAsync(...)
///      ├── GetByTicketIdAsync(...)   ---> GetTicketAttachmentsHandler.HandleAsync(...)
///      ├── GetDownloadUrlAsync(...)  ---> GetAttachmentDownloadUrlHandler.HandleAsync(...)
///      └── DeleteAsync(...)          ---> DeleteAttachmentHandler.HandleAsync(...)
///
/// 2. VẤN ĐỀ TRƯỚC KHI TÁI CẤU TRÚC:
///    - Trước đây file TicketAttachmentsService.cs dài 647 dòng, ôm trọn cả 5 nghiệp vụ (Presign, Confirm,
///      GetList, GetDownloadUrl, Delete) cùng 7 dependencies ở cấp class.
///
/// 3. CÁCH GIẢI QUYẾT (ZERO BREAKING CHANGES):
///    - Tách thành 5 Vertical Slice Handlers độc lập trong `Features/`.
///    - Giữ `TicketAttachmentsService` làm Thin Façade với `[ActivatorUtilitiesConstructor]` cho DI Container
///      và constructor 7 tham số tương thích ngược cho toàn bộ các bài Unit/Integration Test hiện có.
/// </summary>
public class TicketAttachmentsService : ITicketAttachmentsService
{
    private readonly CreatePresignAttachmentHandler _createPresignHandler;
    private readonly ConfirmAttachmentHandler _confirmAttachmentHandler;
    private readonly GetTicketAttachmentsHandler _getTicketAttachmentsHandler;
    private readonly GetAttachmentDownloadUrlHandler _getDownloadUrlHandler;
    private readonly DeleteAttachmentHandler _deleteAttachmentHandler;

    [ActivatorUtilitiesConstructor]
    public TicketAttachmentsService(
        CreatePresignAttachmentHandler createPresignHandler,
        ConfirmAttachmentHandler confirmAttachmentHandler,
        GetTicketAttachmentsHandler getTicketAttachmentsHandler,
        GetAttachmentDownloadUrlHandler getDownloadUrlHandler,
        DeleteAttachmentHandler deleteAttachmentHandler)
    {
        _createPresignHandler = createPresignHandler;
        _confirmAttachmentHandler = confirmAttachmentHandler;
        _getTicketAttachmentsHandler = getTicketAttachmentsHandler;
        _getDownloadUrlHandler = getDownloadUrlHandler;
        _deleteAttachmentHandler = deleteAttachmentHandler;
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ các bài Unit/Integration Test khởi tạo trực tiếp TicketAttachmentsService.
    /// </summary>
    public TicketAttachmentsService(
        AppDbContext context,
        ITicketAttachmentActorContext actorContext,
        IAttachmentStorageService attachmentStorageService,
        IIdentityUserDirectory identityUserDirectory,
        IMaintenanceTicketLookup maintenanceTicketLookup,
        IMaintenanceTicketAccessChecker ticketAccessChecker,
        IOptions<R2AttachmentStorageOptions> storageOptions)
        : this(
            new CreatePresignAttachmentHandler(
                context,
                actorContext,
                attachmentStorageService,
                maintenanceTicketLookup,
                ticketAccessChecker,
                storageOptions),
            new ConfirmAttachmentHandler(
                context,
                actorContext,
                attachmentStorageService,
                identityUserDirectory,
                maintenanceTicketLookup,
                ticketAccessChecker),
            new GetTicketAttachmentsHandler(
                context,
                actorContext,
                identityUserDirectory,
                maintenanceTicketLookup,
                ticketAccessChecker),
            new GetAttachmentDownloadUrlHandler(
                context,
                actorContext,
                attachmentStorageService,
                maintenanceTicketLookup,
                ticketAccessChecker),
            new DeleteAttachmentHandler(
                context,
                actorContext,
                attachmentStorageService,
                maintenanceTicketLookup,
                ticketAccessChecker))
    {
    }

    public Task<ServiceResult<PresignAttachmentResponse>> CreatePresignAsync(
        int ticketId,
        PresignAttachmentRequest request) =>
        _createPresignHandler.HandleAsync(ticketId, request);

    public Task<ServiceResult<TicketAttachmentResponse>> ConfirmAsync(
        int ticketId,
        ConfirmAttachmentRequest request) =>
        _confirmAttachmentHandler.HandleAsync(ticketId, request);

    public Task<ServiceResult<List<TicketAttachmentResponse>>> GetByTicketIdAsync(int ticketId) =>
        _getTicketAttachmentsHandler.HandleAsync(ticketId);

    public Task<ServiceResult<string>> GetDownloadUrlAsync(int ticketId, int attachmentId) =>
        _getDownloadUrlHandler.HandleAsync(ticketId, attachmentId);

    public Task<ServiceResult> DeleteAsync(int ticketId, int attachmentId) =>
        _deleteAttachmentHandler.HandleAsync(ticketId, attachmentId);
}
