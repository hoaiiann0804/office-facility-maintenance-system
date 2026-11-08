using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Organization.Contracts;
using EquipmentEntity = InternalMaintenance.Assets.Domain.Equipment;

namespace InternalMaintenance.Assets.Infrastructure.Features.Common;

/// <summary>
/// Các hàm tiện ích dùng chung (Shared Projection & Cross-Module Lookup) nội bộ trong module Equipment.
/// 
/// [CÁCH GIẢI QUYẾT VẤN ĐỀ - VERTICAL SLICE DRY vs COHESION]:
/// 1. Vấn đề: Khi tách `EfEquipmentCatalog` thành các Feature Slices độc lập (`GetEquipmentPage`, `GetEquipmentById`, `Create`, `Update`),
///    nếu copy-paste biểu thức LINQ `Select` (Projection) và hàm lấy tên phòng ban (`PopulateDepartmentNamesAsync`) vào từng Handler
///    thì khi thêm 1 cột mới vào `EquipmentResponse`, ta phải sửa ở 4 nơi.
/// 2. Giải pháp: Tách phần Projection thuần túy và Cross-Module Lookup với `IOrganizationDirectory` vào `EquipmentProjection`
///    nằm ở `Features/Common` (chỉ `internal static`, không lộ ra ngoài module).
/// 3. Kết quả: Mỗi Feature Handler vẫn độc lập hoàn toàn về luồng nghiệp vụ (Validation, Query, Mutation),
///    nhưng dùng chung biểu thức ánh xạ SQL -> DTO chuẩn xác và chống lỗi N+1 Query.
/// </summary>
internal static class EquipmentProjection
{
    /// <summary>
    /// Chuyển đổi IQueryable&lt;Equipment&gt; sang IQueryable&lt;EquipmentResponse&gt; ngay trên câu lệnh SQL.
    /// EF Core sẽ dịch biểu thức này thành `SELECT Id, Code, Name...` thay vì kéo toàn bộ cột không cần thiết.
    /// </summary>
    public static IQueryable<EquipmentResponse> ProjectToResponse(IQueryable<EquipmentEntity> query) =>
        query.Select(equipment => new EquipmentResponse
        {
            Id = equipment.Id,
            Code = equipment.Code,
            Name = equipment.Name,
            DepartmentId = equipment.DepartmentId,
            // Tên phòng ban thuộc quyền sở hữu của module Organization; khởi tạo rỗng tại đây
            // và điền sau bằng Batch Lookup qua IOrganizationDirectory để không phải JOIN entity chéo module.
            DepartmentName = string.Empty,
            MaintenanceDepartmentId = equipment.MaintenanceDepartmentId,
            MaintenanceDepartmentName = null,
            Status = equipment.Status,
            PurchasedDate = equipment.PurchasedDate,
            Description = equipment.Description,
            CreatedAt = equipment.CreatedAt,
            UpdatedAt = equipment.UpdatedAt
        });

    /// <summary>
    /// Kiểm tra phòng ban có tồn tại hay không thông qua Contract của module Organization.
    /// Giải quyết vấn đề: Tránh việc Equipment truy cập trực tiếp bảng `db.Departments` của module khác,
    /// đồng thời bắt lỗi sớm (trả về 404 rõ ràng) thay vì để SQL Server ném lỗi Foreign Key 500.
    /// </summary>
    public static async Task<bool> DepartmentExistsAsync(
        IOrganizationDirectory organizationDirectory,
        int departmentId,
        CancellationToken cancellationToken)
    {
        var names = await organizationDirectory.FindNamesAsync([departmentId], cancellationToken);
        return names.ContainsKey(departmentId);
    }

    /// <summary>
    /// Điền tên phòng ban sở hữu (`DepartmentName`) và phòng ban bảo trì (`MaintenanceDepartmentName`)
    /// cho danh sách thiết bị bằng đúng 1 lần gọi Batch (`WHERE Id IN (...)`), triệt tiêu hoàn toàn lỗi N+1 Query.
    /// </summary>
    public static async Task PopulateDepartmentNamesAsync(
        IOrganizationDirectory organizationDirectory,
        IReadOnlyCollection<EquipmentResponse> responses,
        CancellationToken cancellationToken)
    {
        if (responses.Count == 0)
            return;

        var departmentIds = responses
            .SelectMany(equipment => equipment.MaintenanceDepartmentId.HasValue
                ? new[] { equipment.DepartmentId, equipment.MaintenanceDepartmentId.Value }
                : new[] { equipment.DepartmentId })
            .Distinct()
            .ToArray();

        var names = await organizationDirectory.FindNamesAsync(departmentIds, cancellationToken);

        foreach (var equipment in responses)
        {
            equipment.DepartmentName = names.GetValueOrDefault(equipment.DepartmentId, string.Empty);
            if (equipment.MaintenanceDepartmentId.HasValue)
                equipment.MaintenanceDepartmentName = names.GetValueOrDefault(equipment.MaintenanceDepartmentId.Value);
        }
    }
}

