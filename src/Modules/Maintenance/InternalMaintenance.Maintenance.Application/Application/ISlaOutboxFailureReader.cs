namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// Cổng đọc trạng thái gửi email để API quản trị không truy cập DbContext trực tiếp.
/// </summary>
public interface ISlaOutboxFailureReader
{
    Task<IReadOnlyList<SlaOutboxFailure>> GetRecentFailuresAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
