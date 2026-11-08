using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Assets.Infrastructure.Features.CreateEquipment;
using InternalMaintenance.Assets.Infrastructure.Features.DeleteEquipment;
using InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentById;
using InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentPage;
using InternalMaintenance.Assets.Infrastructure.Features.UpdateEquipment;
using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Assets.Infrastructure;

/// <summary>
/// [VERTICAL SLICE FACADE - EQUIPMENT CATALOG]
/// 
/// - Vấn đề giải quyết:
///   Trước đây `EquipmentCatalog` là một class "God Service" dài 240+ dòng ôm trọn toàn bộ 5 luồng nghiệp vụ:
///   `GetPageAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` cùng toàn bộ câu lệnh EF Core.
///   Khi nhiều lập trình viên cùng sửa đổi (ví dụ: 1 người sửa bộ lọc tìm kiếm, 1 người sửa rule cập nhật thiết bị),
///   tất cả đều đụng vào cùng một file gây khó đọc và dễ xung đột Git.
/// 
/// - Cách giải quyết:
///   1. Phân rã 5 luồng nghiệp vụ thành 5 Vertical Slice Handlers độc lập trong thư mục `Features/`:
///      - `GetEquipmentPageHandler`
///      - `GetEquipmentByIdHandler`
///      - `CreateEquipmentHandler`
///      - `UpdateEquipmentHandler`
///      - `DeleteEquipmentHandler`
///   2. Biến `EquipmentCatalog` thành một lớp Façade mỏng (Thin Facade) chỉ làm nhiệm vụ điều phối (delegate)
///      xuống đúng Handler của tính năng tương ứng.
///   3. Giữ nguyên giao diện hợp đồng `IEquipmentCatalog` và constructor tương thích ngược để `EquipmentController`
///      cùng toàn bộ bài kiểm thử hiện có hoạt động 100% không có bất kỳ Breaking Change nào.
/// </summary>
public sealed class EquipmentCatalog : IEquipmentCatalog
{
    private readonly GetEquipmentPageHandler _getPageHandler;
    private readonly GetEquipmentByIdHandler _getByIdHandler;
    private readonly CreateEquipmentHandler _createHandler;
    private readonly UpdateEquipmentHandler _updateHandler;
    private readonly DeleteEquipmentHandler _deleteHandler;

    /// <summary>
    /// Constructor chính được DI Container sử dụng để lắp ghép các Vertical Slice Handlers đã đăng ký Scoped.
    /// </summary>
    /// chì dùng 1 Service ckass có từ 2 Conttructor trở lên 
    /// công dụng : Đánh dấu DI Container của .NET biết chính xác phải chọn 
    [ActivatorUtilitiesConstructor]
    public EquipmentCatalog(
        GetEquipmentPageHandler getPageHandler,
        GetEquipmentByIdHandler getByIdHandler,
        CreateEquipmentHandler createHandler,
        UpdateEquipmentHandler updateHandler,
        DeleteEquipmentHandler deleteHandler)
    {
        _getPageHandler = getPageHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ các bài test khởi tạo trực tiếp qua AppDbContext và IOrganizationDirectory.
    /// </summary>
    public EquipmentCatalog(AppDbContext db, IOrganizationDirectory organizationDirectory)
    {
        _getByIdHandler = new GetEquipmentByIdHandler(db, organizationDirectory);
        _getPageHandler = new GetEquipmentPageHandler(db, organizationDirectory);
        _createHandler = new CreateEquipmentHandler(db, organizationDirectory, _getByIdHandler);
        _updateHandler = new UpdateEquipmentHandler(db, organizationDirectory, _getByIdHandler);
        _deleteHandler = new DeleteEquipmentHandler(db);
    }

    public Task<PagedResponse<EquipmentResponse>> GetPageAsync(
        EquipmentQuery query,
        bool isAdmin,
        int? departmentId,
        CancellationToken cancellationToken = default) =>
        _getPageHandler.HandleAsync(query, isAdmin, departmentId, cancellationToken);

    public Task<EquipmentResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _getByIdHandler.HandleAsync(id, cancellationToken);

    public Task<ServiceResult<EquipmentResponse>> CreateAsync(
        CreateEquipmentRequest request,
        CancellationToken cancellationToken = default) =>
        _createHandler.HandleAsync(request, cancellationToken);

    public Task<ServiceResult<EquipmentResponse>> UpdateAsync(
        int id,
        UpdateEquipmentRequest request,
        bool hasOpenTicket,
        CancellationToken cancellationToken = default) =>
        _updateHandler.HandleAsync(id, request, hasOpenTicket, cancellationToken);

    public Task<ServiceResult> DeleteAsync(
        int id,
        bool hasAnyTicket,
        CancellationToken cancellationToken = default) =>
        _deleteHandler.HandleAsync(id, hasAnyTicket, cancellationToken);
}
