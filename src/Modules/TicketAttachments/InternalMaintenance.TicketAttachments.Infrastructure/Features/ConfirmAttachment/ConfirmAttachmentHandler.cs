using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Persistence;
using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.TicketAttachments.Domain;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.ConfirmAttachment;

/// <summary>
/// [VERTICAL SLICE: CONFIRM ATTACHMENT UPLOAD - BƯỚC 2: XÁC NHẬN FILE TRÊN R2 &amp; LƯU METADATA VÀO DB]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/tickets/{ticketId}/attachments/confirm (TicketAttachmentsController)
///           -> ITicketAttachmentsService.ConfirmAsync(ticketId, request)
///           -> ConfirmAttachmentHandler.HandleAsync(ticketId, request)
///           -> Bước 1: Kiểm tra hợp lệ request, quyền truy cập Ticket, trạng thái Ticket và tiền tố `StorageKey`
///              phải thuộc đúng `tickets/{TicketCode}/attachments/`.
///           -> Bước 2: Đối chiếu bản ghi `TicketAttachmentUploadIntent` đã lưu ở Bước 1 (chưa hết hạn, đúng người xin,
///              khớp tên file, loại file và dung lượng).
///           -> Bước 3: Gọi `IAttachmentStorageService.GetObjectMetadataAsync(storageKey)` kiểm tra thực tế trên R2
///              xem file đã được tải lên chưa và `ContentLength` thật sự có khớp với `uploadIntent.FileSize` không.
///           -> Bước 4 (Chống Race Condition với Background Cleanup Worker):
///              Mở SQL Transaction, dùng `ExecuteUpdateAsync` khóa nguyên tử (`ConfirmedAt = UtcNow` khi `CleanupLeaseId == null`),
///              lưu thực thể `TicketAttachment` mới và xóa bản ghi `UploadIntent` trong cùng Transaction.
///           -> Bước 5: Lấy tên hiển thị người tải lên qua `IIdentityUserDirectory.FindDisplayNameAsync` và trả về 201 Created.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Đây là lát cắt phức tạp nhất của module `TicketAttachments` (kết hợp kiểm tra chéo 3 nơi: SQL Intent,
///      Cloudflare R2 HEAD Object Metadata, và Atomic Transaction chống đụng độ với Background Worker).
///    - Đóng gói trọn vẹn vào `ConfirmAttachmentHandler` giúp luồng xác nhận file cực kỳ rõ ràng, dễ bảo trì.
/// </summary>
public sealed class ConfirmAttachmentHandler(
    AppDbContext db,
    ITicketAttachmentActorContext actorContext,
    IAttachmentStorageService attachmentStorageService,
    IIdentityUserDirectory identityUserDirectory,
    IMaintenanceTicketLookup maintenanceTicketLookup,
    IMaintenanceTicketAccessChecker ticketAccessChecker)
{
    public async Task<ServiceResult<TicketAttachmentResponse>> HandleAsync(
        int ticketId,
        ConfirmAttachmentRequest request)
    {
        if (!TryValidateConfirmRequest(request, out var validationError))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                validationError);
        }

        var ticket = await maintenanceTicketLookup.GetAccessContextAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        var currentActor = TicketAttachmentCommon.GetCurrentActor(actorContext);
        if (!await ticketAccessChecker.CanAccessAsync(ticket, currentActor))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "You do not have permission to confirm attachment for this ticket");
        }

        if (!TicketAttachmentRules.IsUploadAllowedStatus(ticket.Status))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Ticket status does not allow attachment confirmation");
        }

        if (!TicketAttachmentRules.IsAllowedContentType(request.ContentType))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "File type is not allowed");
        }

        if (!TicketAttachmentRules.IsAllowedFileSize(request.FileSize))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "File size is invalid");
        }

        var expectedPrefix = TicketAttachmentCommon.BuildAttachmentPrefix(ticket.TicketCode);
        var storageKey = request.StorageKey.Trim();
        if (!storageKey.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "StorageKey does not belong to this ticket");
        }

        var storageKeyExists = await db.TicketAttachments
            .AnyAsync(attachment => attachment.StorageKey == storageKey);

        if (storageKeyExists)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status409Conflict,
                "This file has already been confirmed");
        }

        var now = DateTime.UtcNow;
        var uploadIntent = await db.TicketAttachmentUploadIntents
            .SingleOrDefaultAsync(intent => intent.StorageKey == storageKey &&
                                            intent.MaintenanceTicketId == ticket.TicketId &&
                                            intent.RequestedByUserId == actorContext.UserId &&
                                            intent.ConfirmedAt == null &&
                                            intent.StorageDeletedAt == null);

        if (uploadIntent is null || uploadIntent.UploadUrlExpiresAt <= now)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status409Conflict,
                "Upload intent is missing or expired; request a new upload URL");
        }

        if (!string.Equals(request.OriginalFileName.Trim(), uploadIntent.OriginalFileName, StringComparison.Ordinal) ||
            !string.Equals(request.StoredFileName.Trim(), uploadIntent.StoredFileName, StringComparison.Ordinal) ||
            !string.Equals(request.ContentType.Trim(), uploadIntent.ContentType, StringComparison.OrdinalIgnoreCase) ||
            request.FileSize != uploadIntent.FileSize)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Attachment metadata does not match the upload request");
        }

        var storageMetadataResult = await attachmentStorageService.GetObjectMetadataAsync(storageKey);
        if (!storageMetadataResult.IsSuccess)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                storageMetadataResult.StatusCode,
                storageMetadataResult.Message!);
        }

        if (storageMetadataResult.Data is null)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status404NotFound,
                "Uploaded file was not found in storage");
        }

        var actualFileSize = storageMetadataResult.Data.ContentLength;
        if (!TicketAttachmentRules.IsAllowedFileSize(actualFileSize) ||
            actualFileSize != uploadIntent.FileSize)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Uploaded file size does not match the requested size or exceeds the 100 MB limit");
        }

        var resolvedFileType = TicketAttachmentRules.ResolveFileType(request.ContentType);
        if (!string.IsNullOrWhiteSpace(request.FileType) &&
            !string.Equals(request.FileType.Trim(), resolvedFileType, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "FileType does not match ContentType");
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var reserved = await db.TicketAttachmentUploadIntents
            .Where(intent => intent.Id == uploadIntent.Id &&
                             intent.ConfirmedAt == null &&
                             intent.StorageDeletedAt == null &&
                             intent.CleanupLeaseId == null &&
                             intent.UploadUrlExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(intent => intent.ConfirmedAt, DateTime.UtcNow));

        if (reserved != 1)
        {
            await transaction.RollbackAsync();
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status409Conflict,
                "Upload is expired or already being cleaned up");
        }

        var attachment = new TicketAttachment
        {
            MaintenanceTicketId = ticket.TicketId,
            UploadedByUserId = uploadIntent.RequestedByUserId,
            OriginalFileName = uploadIntent.OriginalFileName,
            StoredFileName = uploadIntent.StoredFileName,
            ContentType = uploadIntent.ContentType,
            FileSize = actualFileSize,
            StorageKey = uploadIntent.StorageKey,
            StorageProvider = "R2",
            FileType = resolvedFileType,
            FileHash = string.IsNullOrWhiteSpace(request.FileHash)
                ? null
                : request.FileHash.Trim()
        };

        db.TicketAttachments.Add(attachment);
        await db.SaveChangesAsync();

        await db.TicketAttachmentUploadIntents
            .Where(intent => intent.Id == uploadIntent.Id && intent.ConfirmedAt != null)
            .ExecuteDeleteAsync();

        await transaction.CommitAsync();

        var uploaderName = await identityUserDirectory.FindDisplayNameAsync(attachment.UploadedByUserId)
            ?? string.Empty;

        return ServiceResult<TicketAttachmentResponse>.Success(
            TicketAttachmentCommon.MapToResponse(attachment, uploaderName),
            StatusCodes.Status201Created);
    }

    private static bool TryValidateConfirmRequest(
        ConfirmAttachmentRequest request,
        out string errorMessage)
    {
        if (request is null)
        {
            errorMessage = "Request body is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.StorageKey) ||
            string.IsNullOrWhiteSpace(request.OriginalFileName) ||
            string.IsNullOrWhiteSpace(request.StoredFileName) ||
            string.IsNullOrWhiteSpace(request.ContentType))
        {
            errorMessage =
                "StorageKey, OriginalFileName, StoredFileName and ContentType are required";
            return false;
        }

        if (!TicketAttachmentRules.IsAllowedFileSize(request.FileSize))
        {
            errorMessage = "File size is invalid";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}

