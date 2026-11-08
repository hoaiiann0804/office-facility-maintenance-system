using InternalMaintenance.Persistence;
using InternalMaintenance.Assets.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Assets.Infrastructure;

/// <summary>
/// Adapter của Assets. Gateway chỉ sửa entity Equipment đang được context theo dõi,
/// không tự gọi SaveChanges; use case Maintenance commit Equipment cùng ticket, vendor log
/// và status history tại một điểm để tránh trạng thái thiết bị lệch với trạng thái ticket.
/// Tính nguyên tử này dựa trên các adapter cùng dùng scoped AppDbContext của request.
/// </summary>
public sealed class EquipmentMaintenanceGateway(AppDbContext db) : IEquipmentMaintenanceGateway
{
    public async Task SetStatusAsync(int equipmentId, string status, CancellationToken cancellationToken = default)
    {
        var equipment = await db.Equipment
            .FirstOrDefaultAsync(item => item.Id == equipmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Equipment {equipmentId} was not found.");

        equipment.Status = status;
    }
}
