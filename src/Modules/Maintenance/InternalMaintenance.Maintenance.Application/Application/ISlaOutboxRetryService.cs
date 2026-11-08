namespace InternalMaintenance.Maintenance.Application;

/// <summary>Cho phép vận hành viên đưa một thông báo thất bại trở lại hàng đợi sau khi sửa nguyên nhân.</summary>
public interface ISlaOutboxRetryService
{
    Task<bool> RequeueFailedAsync(long outboxId, CancellationToken cancellationToken = default);
}
