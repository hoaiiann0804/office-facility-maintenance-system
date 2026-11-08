using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Organization.Infrastructure.Features.Common;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Organization.Infrastructure.Features.UpdateDepartment;

/// <summary>
/// [VERTICAL SLICE: UPDATE DEPARTMENT - CẬP NHẬT THÔNG TIN PHÒNG BAN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> PUT /api/departments/{id} (DepartmentsController)
///           -> IDepartmentCatalog.UpdateAsync(id, request)
///           -> UpdateDepartmentHandler.HandleAsync(id, request)
///           -> Bước 1: Tìm phòng ban theo Id (trả về 404 Not Found nếu không tồn tại).
///           -> Bước 2: Kiểm tra trùng tên với các phòng ban khác (item.Id != id).
///           -> Bước 3: Cập nhật Name, Description, IsMaintenanceTeam và lưu xuống DB.
///           -> Bước 4: Trả về ServiceResult&lt;DepartmentResponse&gt; (200 OK).
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách biệt hoàn toàn logic cập nhật (cần kiểm tra trùng tên loại trừ chính nó `item.Id != id`)
///      khỏi logic tạo mới và xóa.
/// </summary>
public sealed class UpdateDepartmentHandler(AppDbContext db)
{
    public async Task<ServiceResult<DepartmentResponse>> HandleAsync(
        int id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var department = await db.Departments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (department is null)
            return ServiceResult<DepartmentResponse>.Fail(StatusCodes.Status404NotFound, "Department not found");

        var name = request.Name.Trim();
        if (await db.Departments.AnyAsync(item => item.Id != id && item.Name == name, cancellationToken))
            return ServiceResult<DepartmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Department name already exists");

        department.Name = name;
        department.Description = request.Description?.Trim();
        department.IsMaintenanceTeam = request.IsMaintenanceTeam;

        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<DepartmentResponse>.Success(DepartmentProjection.ToResponse(department));
    }
}

