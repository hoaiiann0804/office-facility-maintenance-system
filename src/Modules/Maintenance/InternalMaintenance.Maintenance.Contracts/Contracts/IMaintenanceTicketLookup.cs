namespace InternalMaintenance.Maintenance.Contracts;

/// <summary>
/// Contract để module khác tra cứu ticket mà không truy cập entity hoặc DbSet Maintenance.
/// Boolean dùng cho kiểm tra tồn tại; access context chỉ lộ các trường cần cho quyền/file.
/// </summary>
public interface IMaintenanceTicketLookup
{
    Task<bool> HasEquipmentOccupyingTicketAsync(int equipmentId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyTicketForEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default);
    Task<MaintenanceTicketAccessContext?> GetAccessContextAsync(int ticketId, CancellationToken cancellationToken = default);
}
