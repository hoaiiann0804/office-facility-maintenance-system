using InternalMaintenance.Persistence;
using InternalMaintenance.Assets.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Assets.Infrastructure;

/// <summary>
/// Assets chuyển entity EF thành projection nhỏ. Nhờ vậy Maintenance nhận đúng dữ liệu cần
/// cho use case mà không phải Include hoặc tham chiếu trực tiếp entity Equipment.
/// </summary>
public sealed class EquipmentTicketContextReader(AppDbContext db) : IEquipmentTicketContextReader
{
    public Task<EquipmentTicketContext?> GetAsync(int equipmentId, CancellationToken cancellationToken = default) =>
        db.Equipment
            .AsNoTracking()
            .Where(equipment => equipment.Id == equipmentId)
            .Select(equipment => new EquipmentTicketContext(
                equipment.Status,
                equipment.DepartmentId,
                equipment.MaintenanceDepartmentId,
                equipment.Code,
                equipment.Name))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, EquipmentTicketContext>> GetManyAsync(
        IReadOnlyCollection<int> equipmentIds,
        CancellationToken cancellationToken = default)
    {
        if (equipmentIds.Count == 0)
            return new Dictionary<int, EquipmentTicketContext>();

        // Ticket list cần mã/tên/phòng ban của nhiều thiết bị. Lấy thành một projection theo lô
        // để giữ đúng dữ liệu response mà không buộc Maintenance giữ navigation tới Assets. 
        return await db.Equipment
            .AsNoTracking()
            .Where(equipment => equipmentIds.Contains(equipment.Id))
            .ToDictionaryAsync(
                equipment => equipment.Id,
                equipment => new EquipmentTicketContext(
                    equipment.Status,
                    equipment.DepartmentId,
                    equipment.MaintenanceDepartmentId,
                    equipment.Code,
                    equipment.Name),
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<int>> FindIdsInDepartmentAsync(
        int? departmentId,
        CancellationToken cancellationToken = default)
    {
        // Equipment sở hữu DepartmentId/MaintenanceDepartmentId. Trả ID thay vì entity giúp
        // Tickets áp dụng quyền Manager mà không tham chiếu navigation hoặc kiểu Assets.
        return await db.Equipment
            .AsNoTracking()
            .Where(equipment => equipment.DepartmentId == departmentId ||
                                equipment.MaintenanceDepartmentId == departmentId)
            .Select(equipment => equipment.Id)
            .ToArrayAsync(cancellationToken);
    }

}
