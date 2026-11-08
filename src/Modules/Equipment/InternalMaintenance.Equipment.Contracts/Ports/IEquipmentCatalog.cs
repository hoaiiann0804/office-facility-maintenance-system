using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.BuildingBlocks.Results;
namespace InternalMaintenance.Assets.Contracts;

/// <summary>
/// Use case contract cho CRUD thiết bị. Controller dùng contract này thay vì phụ thuộc
/// AppDbContext, nên cách lưu/truy vấn vẫn nằm bên trong module Equipment.
/// </summary>
public interface IEquipmentCatalog
{
    Task<PagedResponse<EquipmentResponse>> GetPageAsync(
        EquipmentQuery query,
        bool isAdmin,
        int? departmentId,
        CancellationToken cancellationToken = default);

    Task<EquipmentResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<EquipmentResponse>> CreateAsync(
        CreateEquipmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<EquipmentResponse>> UpdateAsync(
        int id,
        UpdateEquipmentRequest request,
        bool hasOpenTicket,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        int id,
        bool hasAnyTicket,
        CancellationToken cancellationToken = default);
}
