using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>Adapter EF lấy các email đã dừng retry để quản trị viên có thể tìm nguyên nhân.</summary>
public sealed class SlaOutboxFailureReader(AppDbContext db) : ISlaOutboxFailureReader
{
    public async Task<IReadOnlyList<SlaOutboxFailure>> GetRecentFailuresAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Giới hạn tại adapter để caller không thể vô tình tải toàn bộ lịch sử lỗi ra bộ nhớ.
        var rows = await (
            from message in db.SlaNotificationOutboxMessages.AsNoTracking()
            join ticket in db.MaintenanceTickets.AsNoTracking()
                on message.MaintenanceTicketId equals ticket.Id
            where message.FailedAt != null
            orderby message.FailedAt descending
            select new SlaOutboxFailure(
                message.Id,
                message.MaintenanceTicketId,
                ticket.TicketCode,
                message.NotificationType,
                message.QueuedAt,
                message.FailedAt!.Value,
                message.AttemptCount,
                message.LastError))
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

        // Dữ liệu cũ có thể chứa Exception.ToString() kèm stack trace; chỉ trả dòng nguyên nhân đầu tiên
        // để endpoint quản trị không làm lộ đường dẫn máy chủ hoặc chi tiết triển khai nội bộ.
        return rows
            .Select(row => row with
            {
                Error = row.Error?.Split(new[] { '\r', '\n' }, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            })
            .ToArray();
    }
}
