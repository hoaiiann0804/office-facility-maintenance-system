using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Organization.Domain;
using InternalMaintenance.Organization.Infrastructure.Features.Common;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Organization.Infrastructure.Features.CreateDepartment;

/// <summary>
/// [VERTICAL SLICE: CREATE DEPARTMENT - TẠO MỚI PHÒNG BAN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/departments (DepartmentsController)
///           -> IDepartmentCatalog.CreateAsync(request)
///           -> CreateDepartmentHandler.HandleAsync(request)
///           -> Bước 1: Chuẩn hóa Name (Trim) và kiểm tra trùng tên trên bảng Departments.
///           -> Bước 2: Khởi tạo thực thể Department và lưu vào cơ sở dữ liệu (SaveChangesAsync).
///           -> Bước 3: Trả về ServiceResult&lt;DepartmentResponse&gt; kèm mã HTTP 201 Created.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Gom trọn vẹn quy tắc nghiệp vụ khi tạo mới phòng ban (kiểm tra trùng tên, gán cờ IsMaintenanceTeam)
///      vào một Slice duy nhất. Khi cần bổ sung rule mới khi tạo phòng ban (ví dụ: mã phòng ban, người quản lý),
///      chỉ cần sửa đúng file này.
/// </summary>
public sealed class CreateDepartmentHandler(AppDbContext db)
{
    public async Task<ServiceResult<DepartmentResponse>> HandleAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await db.Departments.AnyAsync(department => department.Name == name, cancellationToken))
            return ServiceResult<DepartmentResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Department name already exists");

        var department = new Department
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsMaintenanceTeam = request.IsMaintenanceTeam
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<DepartmentResponse>.Success(
            DepartmentProjection.ToResponse(department),
            StatusCodes.Status201Created);
    }
}

