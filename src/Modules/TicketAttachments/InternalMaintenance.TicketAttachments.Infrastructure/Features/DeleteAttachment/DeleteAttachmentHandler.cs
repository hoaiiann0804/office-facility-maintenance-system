using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Persistence;
using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.DeleteAttachment;

/// <summary>
/// [VERTICAL SLICE: DELETE ATTACHMENT - XÓA MỀM TRONG SQL &amp; XÓA VẬT LÝ TRÊN CLOUDFLARE R2 CÓ RETRY]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> DELETE /api/tickets/{ticketId}/attachments/{attachmentId} (TicketAttachmentsController)
///           -> ITicketAttachmentsService.DeleteAsync(ticketId, attachmentId)
///           -> DeleteAttachmentHandler.HandleAsync(ticketId, attachmentId)
///           -> Bước 1: Kiểm tra Ticket tồn tại và quyền xóa của người dùng hiện tại.
///           -> Bước 2: Tìm `TicketAttachment` đang hoạt động (`!IsDeleted`).
///           -> Bước 3 (Ghi nhận ý định xóa bền vững vào SQL trước):
///              Đánh dấu `IsDeleted = true`, `DeletedAt = UtcNow` và `SaveChangesAsync()`.
///              Nhờ vậy, nếu bước gọi R2 bên dưới bị đứt mạng, `AttachmentStorageCleanupWorker` vẫn nhìn thấy
///              bản ghi có `IsDeleted == true &amp;&amp; StorageDeletedAt == null` để tự động dọn dẹp lại.
///           -> Bước 4: Gọi `IAttachmentStorageService.DeleteObjectAsync(attachment.StorageKey)`:
///              + Nếu xóa R2 thành công ngay: Cập nhật `StorageDeletedAt = UtcNow` và trả về `204 NoContent`.
///              + Nếu R2 tạm thời lỗi/timeout: Trả về `202 Accepted` (đã xóa trên giao diện người dùng, Worker sẽ dọn R2 sau).
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Cô lập hoàn toàn cơ chế xóa 2 pha (SQL Soft-Delete + Cloud Object Delete with Worker Fallback)
///      vào một Slice riêng biệt.
/// </summary>
public sealed class DeleteAttachmentHandler(
    AppDbContext db,
    ITicketAttachmentActorContext actorContext,
    IAttachmentStorageService attachmentStorageService,
    IMaintenanceTicketLookup maintenanceTicketLookup,
    IMaintenanceTicketAccessChecker ticketAccessChecker)
{
    public async Task<ServiceResult> HandleAsync(int ticketId, int attachmentId)
    {
        var ticket = await maintenanceTicketLookup.GetAccessContextAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        var currentActor = TicketAttachmentCommon.GetCurrentActor(actorContext);
        if (!await ticketAccessChecker.CanAccessAsync(ticket, currentActor))
        {
            return ServiceResult.Fail(
                StatusCodes.Status403Forbidden,
                "You do not have permission to delete this attachment");
        }

        var attachment = await db.TicketAttachments
            .FirstOrDefaultAsync(item =>
                item.Id == attachmentId &&
                item.MaintenanceTicketId == ticketId &&
                !item.IsDeleted);

        if (attachment is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status404NotFound,
                "Attachment not found");
        }

        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        ServiceResult deleteStorageResult;
        try
        {
            deleteStorageResult = await attachmentStorageService.DeleteObjectAsync(attachment.StorageKey);
        }
        catch (Exception)
        {
            return ServiceResult.Success(StatusCodes.Status202Accepted);
        }

        if (!deleteStorageResult.IsSuccess)
            return ServiceResult.Success(StatusCodes.Status202Accepted);

        attachment.StorageDeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ServiceResult.Success(StatusCodes.Status204NoContent);
    }
}

