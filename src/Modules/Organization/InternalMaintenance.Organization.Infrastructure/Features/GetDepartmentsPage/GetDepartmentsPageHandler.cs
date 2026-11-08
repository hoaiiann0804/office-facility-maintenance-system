using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Organization.Infrastructure.Features.Common;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentsPage;

/// <summary>
/// [VERTICAL SLICE: GET DEPARTMENTS PAGE - TRUY VẤN DANH SÁCH PHÒNG BAN PHÂN TRANG]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/departments (DepartmentsController)
///           -> IDepartmentCatalog.GetPageAsync(query)
///           -> GetDepartmentsPageHandler.HandleAsync(query)
///           -> Lọc theo Keyword & IsMaintenanceTeam trên AppDbContext.Departments (AsNoTracking)
///           -> Đếm tổng số bản ghi (CountAsync) + Phân trang (ApplyPaging) + Ánh xạ SQL (DepartmentProjection)
///           -> Trả về PagedResponse&lt;DepartmentResponse&gt;.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách riêng luồng đọc danh sách có bộ lọc (Filter + Pagination) ra khỏi các luồng ghi (Create/Update/Delete).
///    - Slice này chỉ phụ thuộc duy nhất vào AppDbContext, không cần tiêm IIdentityUserDirectory hay
///      IEquipmentTicketContextReader (vốn chỉ dùng cho luồng DeleteDepartment).
/// </summary>
public sealed class GetDepartmentsPageHandler(AppDbContext db)
{
    public async Task<PagedResponse<DepartmentResponse>> HandleAsync(
        DepartmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var departments = db.Departments.AsNoTracking().AsQueryable();
        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
            departments = departments.Where(department => department.Name.Contains(keyword));

        if (query.IsMaintenanceTeam.HasValue)
            departments = departments.Where(department => department.IsMaintenanceTeam == query.IsMaintenanceTeam.Value);

        var count = await departments.CountAsync(cancellationToken);
        var items = await departments
            .OrderByDescending(department => department.CreatedAt)
            .ThenBy(department => department.Id)
            .ApplyPaging(query)
            .Select(DepartmentProjection.ToResponseProjection())
            .ToListAsync(cancellationToken);

        return items.ToPagedResponse(query, count);
    }
}

