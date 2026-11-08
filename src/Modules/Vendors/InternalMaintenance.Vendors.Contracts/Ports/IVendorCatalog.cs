using InternalMaintenance.BuildingBlocks.Results;

namespace InternalMaintenance.Vendors.Contracts;

/// <summary>Contract CRUD của Vendors; API gọi qua đây thay vì thao tác DbSet trực tiếp.</summary>
public interface IVendorCatalog
{
    Task<IReadOnlyList<VendorResponse>> GetAllAsync(bool? isActive, CancellationToken cancellationToken = default);
    Task<VendorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<VendorResponse>> CreateAsync(CreateVendorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<VendorResponse>> UpdateAsync(int id, UpdateVendorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<VendorResponse>> ToggleActiveAsync(int id, CancellationToken cancellationToken = default);
}
