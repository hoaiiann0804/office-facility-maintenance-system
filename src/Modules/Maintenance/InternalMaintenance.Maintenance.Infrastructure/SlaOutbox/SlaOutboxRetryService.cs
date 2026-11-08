using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>Adapter EF chỉ mở lại thông báo đã thất bại, không cho gửi lại bản ghi đã hoàn tất.</summary>
public sealed class SlaOutboxRetryService(AppDbContext db) : ISlaOutboxRetryService
{
    public async Task<bool> RequeueFailedAsync(long outboxId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Cập nhật có điều kiện để tránh thao tác retry đè lên message vừa được worker hoàn tất.
        // Reset bộ đếm vì đây là một lượt xử lý mới sau khi người vận hành đã khắc phục nguyên nhân.
        var updated = await db.SlaNotificationOutboxMessages
            .Where(message => message.Id == outboxId)
            .Where(message => message.ProcessedAt == null && message.FailedAt != null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.FailedAt, (DateTime?)null)
                .SetProperty(message => message.LastError, (string?)null)
                .SetProperty(message => message.AttemptCount, 0)
                .SetProperty(message => message.NextAttemptAt, now)
            // Retry do Admin yêu cầu là lượt giao nhận mới; đổi mốc enqueue để không dùng lại idempotency key
            // cũ sau khi Resend hết thời gian lưu khóa hoặc khi người vận hành đã sửa payload/người nhận.
            .SetProperty(message => message.QueuedAt, now)
                .SetProperty(message => message.LeaseId, (Guid?)null)
                .SetProperty(message => message.LeaseUntil, (DateTime?)null),
                cancellationToken);

        return updated == 1;
    }
}
