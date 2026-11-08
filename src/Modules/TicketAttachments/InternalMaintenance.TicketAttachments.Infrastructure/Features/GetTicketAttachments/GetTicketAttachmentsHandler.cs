using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Persistence;
using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.GetTicketAttachments;

/// <summary>
/// [VERTICAL SLICE: GET TICKET ATTACHMENTS - LẤY DANH SÁCH FILE ĐÍNH KÈM CỦA PHIẾU BẢO TRÌ]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/tickets/{ticketId}/attachments (TicketAttachmentsController)
///           -> ITicketAttachmentsService.GetByTicketIdAsync(ticketId)
///           -> GetTicketAttachmentsHandler.HandleAsync(ticketId)
///           -> Bước 1: Kiểm tra Ticket tồn tại và quyền xem của Actor hiện tại qua Contract của Maintenance.
///           -> Bước 2: Truy vấn danh sách `TicketAttachments` chưa xóa (`!IsDeleted`) theo `ticketId` (`AsNoTracking`).
///           -> Bước 3: Gom danh sách `UploadedByUserId` duy nhất và gọi `IIdentityUserDirectory.FindDisplayNamesAsync`
///              1 lần (Batch Lookup chống N+1 Query) để điền `UploadedByUserName`.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Slice đọc danh sách này hoàn toàn không chạm tới Cloudflare R2 (`IAttachmentStorageService` hay `R2AttachmentStorageOptions`),
///      giúp giảm thiểu phụ thuộc không cần thiết.
/// </summary>
public sealed class GetTicketAttachmentsHandler(
    AppDbContext db,
    ITicketAttachmentActorContext actorContext,
    IIdentityUserDirectory identityUserDirectory,
    IMaintenanceTicketLookup maintenanceTicketLookup,
    IMaintenanceTicketAccessChecker ticketAccessChecker)
{
    public async Task<ServiceResult<List<TicketAttachmentResponse>>> HandleAsync(int ticketId)
    {
        var ticket = await maintenanceTicketLookup.GetAccessContextAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult<List<TicketAttachmentResponse>>.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        var currentActor = TicketAttachmentCommon.GetCurrentActor(actorContext);
        if (!await ticketAccessChecker.CanAccessAsync(ticket, currentActor))
        {
            return ServiceResult<List<TicketAttachmentResponse>>.Fail(
                StatusCodes.Status403Forbidden,
                "You do not have permission to view attachments of this ticket");
        }

        var attachments = await db.TicketAttachments
            .AsNoTracking()
            .Where(attachment => attachment.MaintenanceTicketId == ticketId && !attachment.IsDeleted)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .Select(attachment => new TicketAttachmentResponse
            {
                Id = attachment.Id,
                MaintenanceTicketId = attachment.MaintenanceTicketId,
                UploadedByUserId = attachment.UploadedByUserId,
                OriginalFileName = attachment.OriginalFileName,
                StoredFileName = attachment.StoredFileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                StorageKey = attachment.StorageKey,
                FileType = attachment.FileType,
                CreatedAt = attachment.CreatedAt,
                IsDeleted = attachment.IsDeleted
            })
            .ToListAsync();

        var uploaderNames = await identityUserDirectory.FindDisplayNamesAsync(
            attachments.Select(attachment => attachment.UploadedByUserId).Distinct().ToArray());

        foreach (var attachment in attachments)
        {
            attachment.UploadedByUserName = uploaderNames.GetValueOrDefault(
                attachment.UploadedByUserId,
                string.Empty);
        }

        return ServiceResult<List<TicketAttachmentResponse>>.Success(attachments);
    }
}

