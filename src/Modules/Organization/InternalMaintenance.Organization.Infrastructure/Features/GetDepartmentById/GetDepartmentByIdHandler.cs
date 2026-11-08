using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Organization.Infrastructure.Features.Common;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentById;

/// <summary>
/// [VERTICAL SLICE: GET DEPARTMENT BY ID - XEM CHI TIẾT PHÒNG BAN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/departments/{id} (DepartmentsController)
///           -> IDepartmentCatalog.GetByIdAsync(id)
///           -> GetDepartmentByIdHandler.HandleAsync(id)
///           -> Truy vấn AppDbContext.Departments (AsNoTracking) theo Id
///           -> Ánh xạ trực tiếp sang DepartmentResponse ngay dưới SQL
///           -> Trả về DepartmentResponse? (null nếu không tìm thấy).
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Cô lập hoàn toàn truy vấn đọc chi tiết theo khóa chính, tối ưu hiệu năng với AsNoTracking()
///      và không kéo theo các dependency liên module không cần thiết.
/// </summary>
public sealed class GetDepartmentByIdHandler(AppDbContext db)
{
    public Task<DepartmentResponse?> HandleAsync(int id, CancellationToken cancellationToken = default) =>
        db.Departments
            .AsNoTracking()
            .Where(department => department.Id == id)
            .Select(DepartmentProjection.ToResponseProjection())
            .FirstOrDefaultAsync(cancellationToken);
}

