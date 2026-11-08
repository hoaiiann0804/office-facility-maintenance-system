using System.Linq.Expressions;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Organization.Domain;

namespace InternalMaintenance.Organization.Infrastructure.Features.Common;

/// <summary>
/// [SHARED CONCERN TRONG NỘI BỘ MODULE ORGANIZATION]
/// Chứa biểu thức ánh xạ (Projection) dùng chung giữa các Slice đọc/ghi trong nội bộ module Organization.
///
/// Giải quyết vấn đề khi tái cấu trúc:
/// - Khi tách EfDepartmentCatalog thành 5 Vertical Slices riêng biệt, việc lặp lại biểu thức Select(...)
///   hoặc hàm ToResponse(...) ở nhiều Slice sẽ gây trùng lặp code.
/// - Gom về Features/Common/DepartmentProjection giúp tái sử dụng logic ánh xạ Entity -> DTO
///   nhưng vẫn đóng gói kín (`internal`) bên trong module Organization.
/// </summary>
internal static class DepartmentProjection
{
    /// <summary>
    /// Biểu thức Expression Tree dùng cho IQueryable.Select(...) để EF Core dịch trực tiếp sang câu lệnh SQL SELECT.
    /// </summary>
    public static Expression<Func<Department, DepartmentResponse>> ToResponseProjection() => department => new DepartmentResponse
    {
        Id = department.Id,
        Name = department.Name,
        Description = department.Description,
        IsMaintenanceTeam = department.IsMaintenanceTeam,
        CreatedAt = department.CreatedAt
    };

    /// <summary>
    /// Ánh xạ trực tiếp trên bộ nhớ (In-Memory) sau khi Create hoặc Update entity Department thành công.
    /// </summary>
    public static DepartmentResponse ToResponse(Department department) => new()
    {
        Id = department.Id,
        Name = department.Name,
        Description = department.Description,
        IsMaintenanceTeam = department.IsMaintenanceTeam,
        CreatedAt = department.CreatedAt
    };
}

