namespace InternalMaintenance.Assets.Contracts;

/// <summary>
/// Contract đọc dữ liệu Equipment tối thiểu cho luồng ticket; không làm lộ entity hay DbContext.
/// </summary>
public interface IEquipmentTicketContextReader
{
    Task<EquipmentTicketContext?> GetAsync(int equipmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, EquipmentTicketContext>> GetManyAsync(
        IReadOnlyCollection<int> equipmentIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<int>> FindIdsInDepartmentAsync(
        int? departmentId,
        CancellationToken cancellationToken = default);
}
