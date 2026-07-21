using System.IO;
using InternalMaintenance.Api.Common.Results;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.TicketAttachments.Contracts;
using InternalMaintenance.Api.Modules.TicketAttachments.Storage;
using InternalMaintenance.Api.Modules.Tickets;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Modules.TicketAttachments;

public class TicketAttachmentsService : ITicketAttachmentsService
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUserService;
    private readonly IAttachmentStorageService _attachmentStorageService;

    public TicketAttachmentsService(
        AppDbContext context,
        CurrentUserService currentUserService,
        IAttachmentStorageService attachmentStorageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _attachmentStorageService = attachmentStorageService;
    }

    public async Task<ServiceResult<PresignAttachmentResponse>> CreatePresignAsync(
        int ticketId,
        PresignAttachmentRequest request)
    {
        // Bước 1 của workflow: kiểm tra request, quyền truy cập và trả về URL upload tạm thời.
        if (!TryValidatePresignRequest(request, out var validationError))
        {
            return ServiceResult<PresignAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                validationError);
        }

        var ticket = await LoadTicketAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult<PresignAttachmentResponse>.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        if (!TicketAccessPolicy.CanAccess(ticket, _currentUserService))
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

        // storedFileName là tên file đã được backend tạo lại để lưu trong storage.
        // Nó ko còn là tên gốc do user upload lên nữa
        // Mục đích : TRánh trùng tên file, giúp file trong storage có tên thống nhất, có thể gắn timestamp + random để dễ duy nhất
        // Ví dụ:  attachment-20260720153010-a1b2c3d4.pdf
        var storedFileName = BuildStoredFileName(request.FileName, request.ContentType);

        // storangeKey là đường dẫn object trong storage.
        // Nó là "khóa" để định danh file trong bucket, không chỉ là tên file
        // ở đây nó được ghép theo ticket:  tickets/{TicketCode}/attachments/{StoredFileName}
        // Ví dụ : tickets/TKT-000123/attachments/attachment-20260720153010-a1b2c3d4.pdf

        // Ý nghĩa nghiệp vụ: 
        // File này thuộc ticket nào thì nằm trong thư mục logic của ticket đó 
        // Dễ quản lý, dễ kiểm tra file có thuộc ticket hay ko
        var storageKey = BuildStorageKey(ticket.TicketCode, storedFileName);

        // uploadUrlResult : là kết quả trả về backend xin presign upload URL từ storage 
        // Nó không phải là file, mà là một object chứa: IsSuccess, StatusCode, Message, Data là chính cái upload URL
        // Backend dùng storagekey + ContentType để tạo url upload tạm thời cho frontend
        var uploadUrlResult = await _attachmentStorageService.CreateUploadUrlAsync(
            storageKey,
            request.ContentType);

        if (!uploadUrlResult.IsSuccess)
        {
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

    public async Task<ServiceResult<TicketAttachmentResponse>> ConfirmAsync(
        int ticketId,
        ConfirmAttachmentRequest request)
    {
        // Bước 2 của workflow: xác nhận file đã lên storage rồi lưu metadata vào DB.
        if (!TryValidateConfirmRequest(request, out var validationError))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                validationError);
        }

        // Dùng LoadTicketAsync để gom toàn bộ logic lấy ticket vào một chỗ và quan trọng hơn là load sẵn dữ liệu liên quan cần cho buiness rule
        var ticket = await LoadTicketAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        if (!TicketAccessPolicy.CanAccess(ticket, _currentUserService))
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

        // Dùng để ràng buộc file upload vào đúng ticket đang xử lý
        // Không cho client thông báo bừa một StorageKey thuộc Ticket khác 
        var expectedPrefix = BuildAttachmentPrefix(ticket.TicketCode);
        // Kiểm tra StorangeKet có bắt đầu bằng frefix ticket hiện tại hay không 
        // OrdinalIgnoreCase: so sánh không phân biệt khoa thường 
        if (!request.StorageKey.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "StorageKey does not belong to this ticket");
        }

        var storageKeyExists = await _context.TicketAttachments
            .AnyAsync(attachment => attachment.StorageKey == request.StorageKey);

        if (storageKeyExists)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status409Conflict,
                "This file has already been confirmed");
        }
        var storageExistsResult = await _attachmentStorageService.ObjectExistsAsync(request.StorageKey);
        if (!storageExistsResult.IsSuccess)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                storageExistsResult.StatusCode,
                storageExistsResult.Message!);
        }

        if (!storageExistsResult.Data)
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status404NotFound,
                "Uploaded file was not found in storage");
        }

        var resolvedFileType = TicketAttachmentRules.ResolveFileType(request.ContentType);
        if (!string.IsNullOrWhiteSpace(request.FileType) &&
            !string.Equals(request.FileType.Trim(), resolvedFileType, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<TicketAttachmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "FileType does not match ContentType");
        }

        var attachment = new TicketAttachment
        {
            MaintenanceTicketId = ticket.Id,
            UploadedByUserId = _currentUserService.UserId,
            OriginalFileName = request.OriginalFileName.Trim(),
            StoredFileName = request.StoredFileName.Trim(),
            ContentType = request.ContentType.Trim(),
            FileSize = request.FileSize,
            StorageKey = request.StorageKey.Trim(),
            StorageProvider = "R2",
            FileType = resolvedFileType,
            FileHash = string.IsNullOrWhiteSpace(request.FileHash)
                ? null
                : request.FileHash.Trim()
        };

        _context.TicketAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        await _context.Entry(attachment)
            .Reference(item => item.UploadedByUser)
            .LoadAsync();

        return ServiceResult<TicketAttachmentResponse>.Success(
            MapToResponse(attachment),
            StatusCodes.Status201Created);
    }

    // Hàm này dùng để : 
    // - Lấy danh sách attachment của ticket
    // - chỉ trả attachment chưa xóa
    // - chỉ trả cho user có quyền 
    public async Task<ServiceResult<List<TicketAttachmentResponse>>> GetByTicketIdAsync(int ticketId)
    {
        var ticket = await LoadTicketAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult<List<TicketAttachmentResponse>>.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        if (!TicketAccessPolicy.CanAccess(ticket, _currentUserService))
        {
            return ServiceResult<List<TicketAttachmentResponse>>.Fail(
                StatusCodes.Status403Forbidden,
                "You do not have permission to view attachments of this ticket");
        }

        var attachments = await _context.TicketAttachments
            .AsNoTracking()
            .Where(attachment => attachment.MaintenanceTicketId == ticketId && !attachment.IsDeleted)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .Select(attachment => new TicketAttachmentResponse
            {
                Id = attachment.Id,
                MaintenanceTicketId = attachment.MaintenanceTicketId,
                UploadedByUserId = attachment.UploadedByUserId,
                UploadedByUserName = attachment.UploadedByUser.FullName,
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

        return ServiceResult<List<TicketAttachmentResponse>>.Success(attachments);
    }

    // Hàm này dùng để : 
    // - lấy đúng 1 attachment
    // - kiểm tra attachment thuộc ticket
    // - sinh URL tải tạm thời từ storage
    // - Ko lộ path storage thật
    public async Task<ServiceResult<string>> GetDownloadUrlAsync(int ticketId, int attachmentId)
    {
        // Trả về URL download tạm thời thay vì lộ trực tiếp đường dẫn storage.
        var ticket = await LoadTicketAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult<string>.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        if (!TicketAccessPolicy.CanAccess(ticket, _currentUserService))
        {
            return ServiceResult<string>.Fail(
                StatusCodes.Status403Forbidden,
                "You do not have permission to download this attachment");
        }

        var attachment = await _context.TicketAttachments
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

        var downloadUrlResult = await _attachmentStorageService.CreateDownloadUrlAsync(
            attachment.StorageKey);

        if (!downloadUrlResult.IsSuccess)
        {
            return ServiceResult<string>.Fail(
                downloadUrlResult.StatusCode,
                downloadUrlResult.Message!);
        }

        return ServiceResult<string>.Success(downloadUrlResult.Data!);
    }

    public async Task<ServiceResult> DeleteAsync(int ticketId, int attachmentId)
    {
        // Xóa file trên storage trước, sau đó mới soft delete record trong DB.
        var ticket = await LoadTicketAsync(ticketId);
        if (ticket is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status404NotFound,
                "Ticket not found");
        }

        if (!TicketAccessPolicy.CanAccess(ticket, _currentUserService))
        {
            return ServiceResult.Fail(
                StatusCodes.Status403Forbidden,
                "You do not have permission to delete this attachment");
        }

        // Tìm attachnent đúng ticket và chưa bị xóa 
        // Điều này đảm bảo:
        // - attachment phải đúng attachmentId
        // - không được xóa lại một record đã soft delete 
        var attachment = await _context.TicketAttachments
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

        // Xóa file thật trên storange trước
        // Điểm quan trọng nhất của flow.
        //Backend xóa file vật lý trước , rồi mới cập nhật DB, vì: 
        // - mục tiêu là file phải biến mất thật khỏi storage
        // - Nếu DB xóa trước mà storange fail, hệ thống sẽ bị lệch trạng thái 
        var deleteStorageResult = await _attachmentStorageService.DeleteObjectAsync(
            attachment.StorageKey);

        if (!deleteStorageResult.IsSuccess)
        {
            return ServiceResult.Fail(
                deleteStorageResult.StatusCode,
                deleteStorageResult.Message!);
        }

        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ServiceResult.Success(StatusCodes.Status204NoContent);
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

    private async Task<MaintenanceTicket?> LoadTicketAsync(int ticketId)
    {
        return await _context.MaintenanceTickets
            .Include(ticket => ticket.Equipment)
            .FirstOrDefaultAsync(ticket => ticket.Id == ticketId);
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

    private static string BuildStoredFileName(string originalFileName, string contentType)
    {
        // Sinh tên lưu trữ duy nhất để tránh trùng file.
        var extension = ResolveFileExtension(originalFileName, contentType);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8];

        return $"attachment-{stamp}-{random}{extension}";
    }

    private static string BuildStorageKey(string ticketCode, string storedFileName)
    {
        // Gom file theo mã ticket để đường dẫn storage dễ quản lý.
        return BuildAttachmentPrefix(ticketCode) + storedFileName;
    }

    private static string BuildAttachmentPrefix(string ticketCode)
    {
        return $"tickets/{ticketCode}/attachments/";
    }

    //Hàm chuẩn hóa extension để backend xin tên file lưu trữ có đuôi đúng và nhất quán
    private static string ResolveFileExtension(string originalFileName, string contentType)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.ToLowerInvariant();
        }

        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            "application/pdf" => ".pdf",
            _ => string.Empty
        };
    }

    // Chuyển entity DB sang response API
    // ý nghĩa: 
    // - client chỉ nhận metadata cần thiết
    // - không lộ navigation data thừa
    // - response format ổn định hơn entity
    private static TicketAttachmentResponse MapToResponse(TicketAttachment attachment)
    {

        return new TicketAttachmentResponse
        {
            Id = attachment.Id,
            MaintenanceTicketId = attachment.MaintenanceTicketId,
            UploadedByUserId = attachment.UploadedByUserId,
            UploadedByUserName = attachment.UploadedByUser?.FullName ?? string.Empty,
            OriginalFileName = attachment.OriginalFileName,
            StoredFileName = attachment.StoredFileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            StorageKey = attachment.StorageKey,
            FileType = attachment.FileType,
            CreatedAt = attachment.CreatedAt,
            IsDeleted = attachment.IsDeleted
        };
    }
}
