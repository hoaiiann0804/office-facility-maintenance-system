using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Organization.Infrastructure.Features.CreateDepartment;
using InternalMaintenance.Organization.Infrastructure.Features.DeleteDepartment;
using InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentById;
using InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentsPage;
using InternalMaintenance.Organization.Infrastructure.Features.UpdateDepartment;
using InternalMaintenance.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Organization.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BỘ ĐIỀU PHỐI MỎNG CHO MODULE ORGANIZATION]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    DepartmentsController -> IDepartmentCatalog (EfDepartmentCatalog)
///      ├── GetPageAsync(...)   ---> GetDepartmentsPageHandler.HandleAsync(...)
///      ├── GetByIdAsync(...)   ---> GetDepartmentByIdHandler.HandleAsync(...)
///      ├── CreateAsync(...)    ---> CreateDepartmentHandler.HandleAsync(...)
///      ├── UpdateAsync(...)    ---> UpdateDepartmentHandler.HandleAsync(...)
///      └── DeleteAsync(...)    ---> DeleteDepartmentHandler.HandleAsync(...)
///
/// 2. VẤN ĐỀ TRƯỚC KHI TÁI CẤU TRÚC:
///    - Trước đây EfDepartmentCatalog vừa gánh toàn bộ CRUD của Department vừa phải tiêm
///      IIdentityUserDirectory và IEquipmentTicketContextReader ở cấp class (dù chỉ hàm Delete mới cần).
///
/// 3. CÁCH GIẢI QUYẾT (ZERO BREAKING CHANGES):
///    - Tách thành 5 Vertical Slice Handlers độc lập trong thư mục Features/.
///    - Chỉ DeleteDepartmentHandler mới nhận dependency liên module (Identity &amp; Equipment).
///    - Giữ EfDepartmentCatalog làm Thin Façade với [ActivatorUtilitiesConstructor] cho DI Container
///      và constructor tương thích ngược cho các bài test hiện hữu.
/// </summary>
public sealed class EfDepartmentCatalog : IDepartmentCatalog
{
    private readonly GetDepartmentsPageHandler _getDepartmentsPageHandler;
    private readonly GetDepartmentByIdHandler _getDepartmentByIdHandler;
    private readonly CreateDepartmentHandler _createDepartmentHandler;
    private readonly UpdateDepartmentHandler _updateDepartmentHandler;
    private readonly DeleteDepartmentHandler _deleteDepartmentHandler;

    [ActivatorUtilitiesConstructor]
    public EfDepartmentCatalog(
        GetDepartmentsPageHandler getDepartmentsPageHandler,
        GetDepartmentByIdHandler getDepartmentByIdHandler,
        CreateDepartmentHandler createDepartmentHandler,
        UpdateDepartmentHandler updateDepartmentHandler,
        DeleteDepartmentHandler deleteDepartmentHandler)
    {
        _getDepartmentsPageHandler = getDepartmentsPageHandler;
        _getDepartmentByIdHandler = getDepartmentByIdHandler;
        _createDepartmentHandler = createDepartmentHandler;
        _updateDepartmentHandler = updateDepartmentHandler;
        _deleteDepartmentHandler = deleteDepartmentHandler;
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ các bài Unit/Integration Test khởi tạo trực tiếp EfDepartmentCatalog.
    /// </summary>
    public EfDepartmentCatalog(
        AppDbContext db,
        IIdentityUserDirectory identityDirectory,
        IEquipmentTicketContextReader equipmentReader)
        : this(
            new GetDepartmentsPageHandler(db),
            new GetDepartmentByIdHandler(db),
            new CreateDepartmentHandler(db),
            new UpdateDepartmentHandler(db),
            new DeleteDepartmentHandler(db, identityDirectory, equipmentReader))
    {
    }

    public Task<PagedResponse<DepartmentResponse>> GetPageAsync(
        DepartmentQuery query,
        CancellationToken cancellationToken = default) =>
        _getDepartmentsPageHandler.HandleAsync(query, cancellationToken);

    public Task<DepartmentResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _getDepartmentByIdHandler.HandleAsync(id, cancellationToken);

    public Task<ServiceResult<DepartmentResponse>> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default) =>
        _createDepartmentHandler.HandleAsync(request, cancellationToken);

    public Task<ServiceResult<DepartmentResponse>> UpdateAsync(
        int id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default) =>
        _updateDepartmentHandler.HandleAsync(id, request, cancellationToken);

    public Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _deleteDepartmentHandler.HandleAsync(id, cancellationToken);
}
