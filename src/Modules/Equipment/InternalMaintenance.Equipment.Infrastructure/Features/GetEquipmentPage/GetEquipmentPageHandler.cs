using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Assets.Infrastructure.Features.Common;
using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentPage;

/// <summary>
/// [VERTICAL SLICE - FEATURE: LẤY DANH SÁCH THIẾT BỊ PHÂN TRANG (GET EQUIPMENT PAGE)]
/// 
/// - Vấn đề giải quyết:
///   Trước đây logic tìm kiếm, lọc theo quyền phòng ban và phân trang nằm chung với các hàm thêm/sửa/xóa
///   trong class `EquipmentCatalog` dài 240 dòng. Khi cần tối ưu câu lệnh tìm kiếm hoặc thêm bộ lọc mới,
///   dev phải sửa chung 1 file lớn dễ gây xung đột Git (Merge Conflict).
/// 
/// - Cách giải quyết:
///   Tách riêng thành `GetEquipmentPageHandler` chuyên trách duy nhất luồng đọc danh sách thiết bị:
///   1. Dùng `AsNoTracking()` để EF Core không tạo snapshot theo dõi thực thể, giảm RAM và tăng tốc độ đọc.
///   2. Áp dụng phân quyền dữ liệu (Row-level security): Nếu không phải Admin, chỉ lấy thiết bị thuộc phòng ban
///      của user hoặc do phòng ban của user phụ trách bảo trì.
///   3. Lọc theo từ khóa (`Keyword`), trạng thái (`Status`), phòng ban (`DepartmentId`) trên SQL.
///   4. Dùng `EquipmentProjection.PopulateDepartmentNamesAsync` lấy tên phòng ban theo lô (Batch) chống N+1 Query.
/// </summary>
public sealed class GetEquipmentPageHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory)
{
    public async Task<PagedResponse<EquipmentResponse>> HandleAsync(
        EquipmentQuery query,
        bool isAdmin,
        int? departmentId,
        CancellationToken cancellationToken = default)
    {
        var equipmentQuery = db.Equipment.AsNoTracking().AsQueryable();

        if (!isAdmin)
        {
            // Quyền xem theo phạm vi phòng ban được truyền từ tầng API xuống;
            // áp dụng lọc ngay trên IQueryable để SQL Server chỉ quét các dòng trong phạm vi được phép.
            equipmentQuery = equipmentQuery.Where(equipment =>
                equipment.DepartmentId == departmentId || equipment.MaintenanceDepartmentId == departmentId);
        }

        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            equipmentQuery = equipmentQuery.Where(equipment =>
                equipment.Name.Contains(keyword) || equipment.Code.Contains(keyword) ||
                (equipment.Description != null && equipment.Description.Contains(keyword)));
        }

        var status = query.Status?.Trim();
        if (!string.IsNullOrWhiteSpace(status))
            equipmentQuery = equipmentQuery.Where(equipment => equipment.Status == status);

        if (query.DepartmentId.HasValue)
            equipmentQuery = equipmentQuery.Where(equipment => equipment.DepartmentId == query.DepartmentId.Value);

        var totalItems = await equipmentQuery.CountAsync(cancellationToken);
        var items = await EquipmentProjection.ProjectToResponse(
                equipmentQuery
                    .OrderByDescending(equipment => equipment.CreatedAt)
                    .ThenBy(equipment => equipment.Id)
                    .ApplyPaging(query))
            .ToListAsync(cancellationToken);

        await EquipmentProjection.PopulateDepartmentNamesAsync(organizationDirectory, items, cancellationToken);

        return items.ToPagedResponse(query, totalItems);
    }
}

