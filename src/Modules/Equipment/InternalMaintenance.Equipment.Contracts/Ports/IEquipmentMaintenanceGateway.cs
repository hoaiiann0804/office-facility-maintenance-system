namespace InternalMaintenance.Assets.Contracts;

/// <summary>
/// Cổng để Maintenance yêu cầu Equipment đổi trạng thái thiết bị.
/// Adapter phía Equipment chỉ sửa entity đang được theo dõi, không tự commit;
/// nhờ vậy cập nhật ticket và thiết bị vẫn nằm trong transaction dùng chung hiện tại.
/// </summary>
public interface IEquipmentMaintenanceGateway
{
    Task SetStatusAsync(int equipmentId, string status, CancellationToken cancellationToken = default);
}
