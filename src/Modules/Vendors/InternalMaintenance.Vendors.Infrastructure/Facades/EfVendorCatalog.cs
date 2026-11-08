using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Infrastructure.Features.CreateVendor;
using InternalMaintenance.Vendors.Infrastructure.Features.GetVendorById;
using InternalMaintenance.Vendors.Infrastructure.Features.GetVendors;
using InternalMaintenance.Vendors.Infrastructure.Features.ToggleVendorActive;
using InternalMaintenance.Vendors.Infrastructure.Features.UpdateVendor;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Vendors.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BỘ ĐIỀU PHỐI MỎNG CHO MODULE VENDORS]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    VendorsController -> IVendorCatalog (EfVendorCatalog)
///      ├── GetAllAsync(...)        ---> GetVendorsHandler.HandleAsync(...)
///      ├── GetByIdAsync(...)       ---> GetVendorByIdHandler.HandleAsync(...)
///      ├── CreateAsync(...)        ---> CreateVendorHandler.HandleAsync(...)
///      ├── UpdateAsync(...)        ---> UpdateVendorHandler.HandleAsync(...)
///      └── ToggleActiveAsync(...)  ---> ToggleVendorActiveHandler.HandleAsync(...)
///
/// 2. VẤN ĐỀ TRƯỚC KHI TÁI CẤU TRÚC:
///    - Trước đây EfVendorCatalog ôm toàn bộ 5 nghiệp vụ (Read list, Read detail, Create, Update, ToggleActive)
///      cùng các hàm ánh xạ và kiểm tra trùng tên vào chung một file.
///
/// 3. CÁCH GIẢI QUYẾT (ZERO BREAKING CHANGES):
///    - Tách mỗi use-case thành 1 Vertical Slice Handler độc lập trong thư mục Features/.
///    - Giữ EfVendorCatalog làm Thin Façade với 2 constructor:
///      + Constructor chính gắn [ActivatorUtilitiesConstructor] để DI Container tiêm 5 Slice Handlers.
///      + Constructor phụ nhận (AppDbContext db) để tương thích ngược 100% với các bài test khởi tạo trực tiếp.
/// </summary>
public sealed class EfVendorCatalog : IVendorCatalog
{
    private readonly GetVendorsHandler _getVendorsHandler;
    private readonly GetVendorByIdHandler _getVendorByIdHandler;
    private readonly CreateVendorHandler _createVendorHandler;
    private readonly UpdateVendorHandler _updateVendorHandler;
    private readonly ToggleVendorActiveHandler _toggleVendorActiveHandler;

    [ActivatorUtilitiesConstructor]
    public EfVendorCatalog(
        GetVendorsHandler getVendorsHandler,
        GetVendorByIdHandler getVendorByIdHandler,
        CreateVendorHandler createVendorHandler,
        UpdateVendorHandler updateVendorHandler,
        ToggleVendorActiveHandler toggleVendorActiveHandler)
    {
        _getVendorsHandler = getVendorsHandler;
        _getVendorByIdHandler = getVendorByIdHandler;
        _createVendorHandler = createVendorHandler;
        _updateVendorHandler = updateVendorHandler;
        _toggleVendorActiveHandler = toggleVendorActiveHandler;
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ các bài Unit/Integration Test khởi tạo trực tiếp EfVendorCatalog.
    /// </summary>
    public EfVendorCatalog(AppDbContext db)
        : this(
            new GetVendorsHandler(db),
            new GetVendorByIdHandler(db),
            new CreateVendorHandler(db),
            new UpdateVendorHandler(db),
            new ToggleVendorActiveHandler(db))
    {
    }

    public Task<IReadOnlyList<VendorResponse>> GetAllAsync(
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        _getVendorsHandler.HandleAsync(isActive, cancellationToken);

    public Task<VendorResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _getVendorByIdHandler.HandleAsync(id, cancellationToken);

    public Task<ServiceResult<VendorResponse>> CreateAsync(
        CreateVendorRequest request,
        CancellationToken cancellationToken = default) =>
        _createVendorHandler.HandleAsync(request, cancellationToken);

    public Task<ServiceResult<VendorResponse>> UpdateAsync(
        int id,
        UpdateVendorRequest request,
        CancellationToken cancellationToken = default) =>
        _updateVendorHandler.HandleAsync(id, request, cancellationToken);

    public Task<ServiceResult<VendorResponse>> ToggleActiveAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _toggleVendorActiveHandler.HandleAsync(id, cancellationToken);
}
