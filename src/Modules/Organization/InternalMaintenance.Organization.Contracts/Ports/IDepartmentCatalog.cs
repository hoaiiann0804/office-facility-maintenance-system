using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.BuildingBlocks.Results;

namespace InternalMaintenance.Organization.Contracts;

/// <summary>
/// Contract quản lý phòng ban; host và module gọi qua projection/DTO thay vì truy vấn entity Department trực tiếp.
/// </summary>
public interface IDepartmentCatalog
{
    Task<PagedResponse<DepartmentResponse>> GetPageAsync(DepartmentQuery query, CancellationToken cancellationToken = default);
    Task<DepartmentResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<DepartmentResponse>> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<DepartmentResponse>> UpdateAsync(int id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
