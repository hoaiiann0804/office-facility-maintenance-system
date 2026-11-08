using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Organization.Infrastructure.Features.DeleteDepartment;

/// <summary>
/// [VERTICAL SLICE: DELETE DEPARTMENT - XÓA PHÒNG BAN &amp; KIỂM TRA RÀNG BUỘC LIÊN MODULE]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> DELETE /api/departments/{id} (DepartmentsController)
///           -> IDepartmentCatalog.DeleteAsync(id)
///           -> DeleteDepartmentHandler.HandleAsync(id)
///           -> Bước 1: Kiểm tra phòng ban có tồn tại trong bảng Departments hay không (404 nếu không có).
///           -> Bước 2 (Giao tiếp liên module qua Contracts):
///              + Gọi IIdentityUserDirectory.FindUserIdsByDepartmentAsync(id) sang module Identity.
///              + Gọi IEquipmentTicketContextReader.FindIdsInDepartmentAsync(id) sang module Equipment.
///              + Nếu còn nhân sự hoặc thiết bị thuộc phòng ban này -> Từ chối xóa (400 Bad Request).
///           -> Bước 3: Xóa bản ghi Department và lưu thay đổi (SaveChangesAsync).
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Trước đây EfDepartmentCatalog phải nhận IIdentityUserDirectory và IEquipmentTicketContextReader
///      ở cấp độ toàn class dù 4 hàm còn lại (GetPage, GetById, Create, Update) hoàn toàn không dùng tới.
///    - Khi tách thành Vertical Slice, CHỈ DUY NHẤT DeleteDepartmentHandler mới yêu cầu 2 cổng giao tiếp
///      liên module này. Điều này giúp giảm phụ thuộc (coupling) và làm rõ ranh giới giao tiếp giữa
///      module Organization với Identity và Equipment.
/// </summary>
public sealed class DeleteDepartmentHandler(
    AppDbContext db,
    IIdentityUserDirectory identityDirectory,
    IEquipmentTicketContextReader equipmentReader)
{
    public async Task<ServiceResult> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await db.Departments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (department is null)
            return ServiceResult.Fail(StatusCodes.Status404NotFound, "Department not found");

        // Organization không truy vấn trực tiếp DbSet<User> hay DbSet<Equipment> của module khác.
        // Thay vào đó, gọi qua Public Contract của từng module để kiểm tra ràng buộc toàn vẹn.
        var userIds = await identityDirectory.FindUserIdsByDepartmentAsync(id, cancellationToken);
        var equipmentIds = await equipmentReader.FindIdsInDepartmentAsync(id, cancellationToken);
        if (userIds.Count > 0 || equipmentIds.Count > 0)
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                "Cannot delete department because it has related users or equipment");

        db.Departments.Remove(department);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}

