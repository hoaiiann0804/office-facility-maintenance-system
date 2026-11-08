using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Persistence;
using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.TicketAttachments.Domain;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.CreatePresign;

/// <summary>
/// [VERTICAL SLICE: CREATE PRESIGN ATTACHMENT URL - BƯỚC 1: XIN URL UPLOAD TẠM THỜI LÊN R2/S3]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/tickets/{ticketId}/attachments/presign (TicketAttachmentsController)
///           -> ITicketAttachmentsService.CreatePresignAsync(ticketId, request)
///           -> CreatePresignAttachmentHandler.HandleAsync(ticketId, request)
///           -> Bước 1: Kiểm tra hợp lệ đầu vào (FileName, ContentType, FileSize &lt;= giới hạn cho phép).
///           -> Bước 2: Hỏi module Maintenance qua IMaintenanceTicketLookup &amp; IMaintenanceTicketAccessChecker
///              xem Ticket có tồn tại, người dùng hiện tại có quyền truy cập và trạng thái Ticket có cho phép đính kèm file không.
///           -> Bước 3: Sinh `storedFileName` duy nhất và `storageKey` theo cấu trúc `tickets/{TicketCode}/attachments/{storedFileName}`.
///           -> Bước 4 (Đảm bảo chống file mồ côi trên Cloud Storage):
///              Lưu bản ghi `TicketAttachmentUploadIntent` vào SQL Server TRƯỚC khi cấp Presigned PUT URL.
///              Nếu trình duyệt upload lên R2 xong nhưng mất mạng không gọi Confirm, Background Worker vẫn có
///              `StorageKey` trong bảng Intent để dọn dẹp object mồ côi sau khi hết hạn.
///           -> Bước 5: Gọi IAttachmentStorageService.CreateUploadUrlAsync(...) lấy Presigned PUT URL trả về cho Client.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách riêng bước khởi tạo Upload Intent &amp; xin Presigned PUT URL. Slice này không cần phụ thuộc
///      vào `IIdentityUserDirectory` (vốn chỉ dùng khi hiển thị tên người tải lên ở bước Confirm và GetList).
/// </summary>
public sealed class CreatePresignAttachmentHandler(
    AppDbContext db,
    ITicketAttachmentActorContext actorContext,
    IAttachmentStorageService attachmentStorageService,
    IMaintenanceTicketLookup maintenanceTicketLookup,
    IMaintenanceTicketAccessChecker ticketAccessChecker,
    IOptions<R2AttachmentStorageOptions> storageOptions)
{
    private readonly R2AttachmentStorageOptions _storageOptions = storageOptions.Value;

    public async Task<ServiceResult<PresignAttachmentResponse>> HandleAsync(
        int ticketId,
        PresignAttachmentRequest request)
    {
        if (!TryValidatePresignRequest(request, out var validationError))
        {
            return ServiceResult<PresignAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                validationError);
        }

        var ticket = await maintenanceTicketLookup.GetAccessContextAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult<PresignAttachmentResponse>.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        var currentActor = TicketAttachmentCommon.GetCurrentActor(actorContext);
        if (!await ticketAccessChecker.CanAccessAsync(ticket, currentActor))
        {
            return ServiceResult<PresignAttachmentResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "You do not have permission to add attachment to this ticket");
        }

        if (!TicketAttachmentRules.IsUploadAllowedStatus(ticket.Status))
        {
            return ServiceResult<PresignAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Ticket status does not allow file upload");
        }

        if (!TicketAttachmentRules.IsAllowedContentType(request.ContentType))
        {
            return ServiceResult<PresignAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "File type is not allowed");
        }

        var storedFileName = TicketAttachmentCommon.BuildStoredFileName(request.FileName, request.ContentType);
        var storageKey = TicketAttachmentCommon.BuildStorageKey(ticket.TicketCode, storedFileName);

        var uploadUrlExpiresAt = DateTime.UtcNow.AddMinutes(
            Math.Clamp(_storageOptions.PresignExpiresMinutes, 1, 7 * 24 * 60));

        var uploadIntent = new TicketAttachmentUploadIntent
        {
            MaintenanceTicketId = ticket.TicketId,
            RequestedByUserId = actorContext.UserId,
            StorageKey = storageKey,
            OriginalFileName = request.FileName.Trim(),
            StoredFileName = storedFileName,
            ContentType = request.ContentType.Trim(),
            FileSize = request.FileSize,
            UploadUrlExpiresAt = uploadUrlExpiresAt
        };

        db.TicketAttachmentUploadIntents.Add(uploadIntent);
        await db.SaveChangesAsync();

        var uploadUrlResult = await attachmentStorageService.CreateUploadUrlAsync(
            storageKey,
            request.ContentType);

        if (!uploadUrlResult.IsSuccess)
        {
            db.TicketAttachmentUploadIntents.Remove(uploadIntent);
            await db.SaveChangesAsync();
            return ServiceResult<PresignAttachmentResponse>.Fail(
                uploadUrlResult.StatusCode,
                uploadUrlResult.Message!);
        }

        return ServiceResult<PresignAttachmentResponse>.Success(
            new PresignAttachmentResponse
            {
                UploadUrl = uploadUrlResult.Data!,
                StorageKey = storageKey,
                StoredFileName = storedFileName
            });
    }

    private static bool TryValidatePresignRequest(
        PresignAttachmentRequest request,
        out string errorMessage)
    {
        if (request is null)
        {
            errorMessage = "Request body is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.FileName) ||
            string.IsNullOrWhiteSpace(request.ContentType))
        {
            errorMessage = "FileName and ContentType are required";
            return false;
        }

        if (!TicketAttachmentRules.IsAllowedFileSize(request.FileSize))
        {
            errorMessage =
                $"File size is invalid or exceeds {TicketAttachmentRules.MaxUploadFileSizeBytes / 1024 / 1024} MB";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}

