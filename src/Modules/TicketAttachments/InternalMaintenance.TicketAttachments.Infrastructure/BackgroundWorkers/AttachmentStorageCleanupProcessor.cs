using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternalMaintenance.TicketAttachments.Infrastructure;

/// <summary>
/// Một lượt xử lý hàng đợi xóa storage. Tách processor khỏi lịch chạy để có thể kiểm thử
/// rõ tình huống storage lỗi rồi retry mà không phải chờ chu kỳ BackgroundService thật.
/// </summary>
public sealed class AttachmentStorageCleanupProcessor(
    AppDbContext db,
    IAttachmentStorageService storage,
    ILogger<AttachmentStorageCleanupProcessor> logger)
{
    private const int BatchSize = 50;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan UploadCompletionGracePeriod = TimeSpan.FromMinutes(30);

    public async Task ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var pending = await db.TicketAttachments
            .AsNoTracking()
            .Where(attachment => attachment.IsDeleted &&
                                 attachment.StorageDeletedAt == null &&
                                 (attachment.StorageDeleteLeaseUntil == null ||
                                  attachment.StorageDeleteLeaseUntil <= DateTime.UtcNow))
            .OrderBy(attachment => attachment.DeletedAt)
            .Take(BatchSize)
            .Select(attachment => new { attachment.Id, attachment.StorageKey })
            .ToListAsync(cancellationToken);

        foreach (var item in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var leaseId = Guid.NewGuid();
            var leaseNow = DateTime.UtcNow;

            // Đọc batch chỉ tìm ứng viên, không giành quyền xử lý. UPDATE có điều kiện mới là
            // thao tác claim nguyên tử: nếu instance khác vừa claim, số dòng cập nhật sẽ là 0.
            var claimed = await db.TicketAttachments
                .Where(attachment => attachment.Id == item.Id &&
                                     attachment.IsDeleted &&
                                     attachment.StorageDeletedAt == null &&
                                     (attachment.StorageDeleteLeaseUntil == null ||
                                      attachment.StorageDeleteLeaseUntil <= leaseNow))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(attachment => attachment.StorageDeleteLeaseId, leaseId)
                    .SetProperty(attachment => attachment.StorageDeleteLeaseUntil, leaseNow.Add(LeaseDuration)),
                    cancellationToken);

            if (claimed != 1)
                continue;

            try
            {
                var result = await storage.DeleteObjectAsync(item.StorageKey);
                if (!result.IsSuccess)
                {
                    // Không đánh dấu hoàn tất khi storage từ chối. Nhả lease để retry được ngay
                    // ở lượt sau; nếu process chết thì lease tự hết hạn sau 5 phút.
                    await ReleaseLeaseAsync(item.Id, leaseId, cancellationToken);
                    logger.LogWarning("Chưa xóa được attachment {AttachmentId} khỏi storage: {Message}",
                        item.Id, result.Message);
                    continue;
                }

                // Chỉ chủ lease hiện tại được ghi nhận hoàn tất. Nếu process dừng sau khi R2 xóa
                // nhưng trước UPDATE này, lease hết hạn và lần sau lặp DELETE an toàn (idempotent).
                await db.TicketAttachments
                    .Where(attachment => attachment.Id == item.Id &&
                                         attachment.StorageDeletedAt == null &&
                                         attachment.StorageDeleteLeaseId == leaseId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(attachment => attachment.StorageDeletedAt, DateTime.UtcNow)
                        .SetProperty(attachment => attachment.StorageDeleteLeaseId, (Guid?)null)
                        .SetProperty(attachment => attachment.StorageDeleteLeaseUntil, (DateTime?)null),
                        cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Nhả lease khi có lỗi để bản ghi vẫn pending và không phải đợi hết thời hạn.
                await ReleaseLeaseAsync(item.Id, leaseId, cancellationToken);
                // Lỗi một object không được ngăn các item sau; bản ghi lỗi vẫn pending để retry.
                logger.LogWarning(exception, "Lỗi khi dọn attachment {AttachmentId}; sẽ thử lại ở lượt sau.",
                    item.Id);
            }
        }

        await ProcessExpiredUploadIntentsAsync(cancellationToken);
    }

    private async Task ProcessExpiredUploadIntentsAsync(CancellationToken cancellationToken)
    {
        // Confirm từ chối URL đã hết hạn; do đó 24 giờ không phải thời gian retry confirm mà chỉ giữ
        // object mồ côi quá lâu. Chờ 30 phút sau hạn URL để request PUT bắt đầu sát giờ hết hạn có
        // thời gian hoàn tất, rồi dọn intent chưa confirm để file quá cỡ/upload bỏ dở không tồn tại lâu.
        // Intent đã confirm không bị chọn, kể cả file attachment còn đang sử dụng.
        var cleanupBefore = DateTime.UtcNow.Subtract(UploadCompletionGracePeriod);
        var pending = await db.TicketAttachmentUploadIntents
            .AsNoTracking()
            .Where(intent => intent.ConfirmedAt == null &&
                             intent.StorageDeletedAt == null &&
                             intent.UploadUrlExpiresAt <= cleanupBefore &&
                             (intent.CleanupLeaseUntil == null || intent.CleanupLeaseUntil <= DateTime.UtcNow))
            .OrderBy(intent => intent.UploadUrlExpiresAt)
            .Take(BatchSize)
            .Select(intent => new { intent.Id, intent.StorageKey })
            .ToListAsync(cancellationToken);

        foreach (var item in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var leaseId = Guid.NewGuid();
            var leaseNow = DateTime.UtcNow;
            var claimed = await db.TicketAttachmentUploadIntents
                .Where(intent => intent.Id == item.Id &&
                                 intent.ConfirmedAt == null &&
                                 intent.StorageDeletedAt == null &&
                                 intent.UploadUrlExpiresAt <= cleanupBefore &&
                                 (intent.CleanupLeaseUntil == null || intent.CleanupLeaseUntil <= leaseNow))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(intent => intent.CleanupLeaseId, leaseId)
                    .SetProperty(intent => intent.CleanupLeaseUntil, leaseNow.Add(LeaseDuration)),
                    cancellationToken);

            if (claimed != 1)
                continue;

            try
            {
                var result = await storage.DeleteObjectAsync(item.StorageKey);
                if (!result.IsSuccess)
                {
                    await ReleaseUploadIntentLeaseAsync(item.Id, leaseId, cancellationToken);
                    logger.LogWarning("Chưa dọn được file upload chưa xác nhận {UploadIntentId}: {Message}",
                        item.Id, result.Message);
                    continue;
                }

                await db.TicketAttachmentUploadIntents
                    .Where(intent => intent.Id == item.Id &&
                                     intent.ConfirmedAt == null &&
                                     intent.CleanupLeaseId == leaseId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(intent => intent.StorageDeletedAt, DateTime.UtcNow)
                        .SetProperty(intent => intent.CleanupLeaseId, (Guid?)null)
                        .SetProperty(intent => intent.CleanupLeaseUntil, (DateTime?)null),
                        cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                await ReleaseUploadIntentLeaseAsync(item.Id, leaseId, cancellationToken);
                logger.LogWarning(exception, "Lỗi khi dọn upload intent {UploadIntentId}; worker sẽ thử lại.",
                    item.Id);
            }
        }
    }

    private Task<int> ReleaseLeaseAsync(int attachmentId, Guid leaseId, CancellationToken cancellationToken) =>
        db.TicketAttachments
            .Where(attachment => attachment.Id == attachmentId &&
                                 attachment.StorageDeleteLeaseId == leaseId &&
                                 attachment.StorageDeletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attachment => attachment.StorageDeleteLeaseId, (Guid?)null)
                .SetProperty(attachment => attachment.StorageDeleteLeaseUntil, (DateTime?)null),
                cancellationToken);

    private Task<int> ReleaseUploadIntentLeaseAsync(int uploadIntentId, Guid leaseId, CancellationToken cancellationToken) =>
        db.TicketAttachmentUploadIntents
            .Where(intent => intent.Id == uploadIntentId &&
                             intent.CleanupLeaseId == leaseId &&
                             intent.StorageDeletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(intent => intent.CleanupLeaseId, (Guid?)null)
                .SetProperty(intent => intent.CleanupLeaseUntil, (DateTime?)null),
                cancellationToken);
}
