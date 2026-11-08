using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Persistence;
using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.GetDownloadUrl;

/// <summary>
/// [VERTICAL SLICE: GET ATTACHMENT DOWNLOAD URL - CẤP PRESIGNED GET URL ĐỂ TẢI FILE AN TOÀN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/tickets/{ticketId}/attachments/{attachmentId}/download-url (TicketAttachmentsController)
///           -> ITicketAttachmentsService.GetDownloadUrlAsync(ticketId, attachmentId)
///           -> GetAttachmentDownloadUrlHandler.HandleAsync(ticketId, attachmentId)
///           -> Bước 1: Kiểm tra Ticket tồn tại và quyền truy cập của người dùng hiện tại.
///           -> Bước 2: Tìm bản ghi `TicketAttachment` khớp cả `attachmentId` và `ticketId` (chưa bị xóa mềm).
///           -> Bước 3: Gọi `IAttachmentStorageService.CreateDownloadUrlAsync(attachment.StorageKey)` để sinh
///              Presigned GET URL có thời hạn ngắn mà không làm lộ đường dẫn bucket nội bộ.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách riêng luồng cấp link tải file. Slice này không cần phụ thuộc vào `IIdentityUserDirectory`.
/// </summary>
public sealed class GetAttachmentDownloadUrlHandler(
    AppDbContext db,
    ITicketAttachmentActorContext actorContext,
    IAttachmentStorageService attachmentStorageService,
    IMaintenanceTicketLookup maintenanceTicketLookup,
    IMaintenanceTicketAccessChecker ticketAccessChecker)
{
    public async Task<ServiceResult<string>> HandleAsync(int ticketId, int attachmentId)
    {
        var ticket = await maintenanceTicketLookup.GetAccessContextAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult<string>.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        var currentActor = TicketAttachmentCommon.GetCurrentActor(actorContext);
        if (!await ticketAccessChecker.CanAccessAsync(ticket, currentActor))
        {
            return ServiceResult<string>.Fail(
                StatusCodes.Status403Forbidden,
                "You do not have permission to download this attachment");
        }

        var attachment = await db.TicketAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == attachmentId &&
                item.MaintenanceTicketId == ticketId &&
                !item.IsDeleted);

        if (attachment is null)
        {
            return ServiceResult<string>.Fail(
                StatusCodes.Status404NotFound,
                "Attachment not found");
        }

        var downloadUrlResult = await attachmentStorageService.CreateDownloadUrlAsync(
            attachment.StorageKey);

        if (!downloadUrlResult.IsSuccess)
        {
            return ServiceResult<string>.Fail(
                downloadUrlResult.StatusCode,
                downloadUrlResult.Message!);
        }

        return ServiceResult<string>.Success(downloadUrlResult.Data!);
    }
}

