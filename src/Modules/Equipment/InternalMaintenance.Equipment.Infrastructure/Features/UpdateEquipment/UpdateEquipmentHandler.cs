using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Assets.Infrastructure.Features.Common;
using InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentById;
using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Assets.Infrastructure.Features.UpdateEquipment;

/// <summary>
/// [VERTICAL SLICE - FEATURE: CẬP NHẬT THIẾT BỊ (UPDATE EQUIPMENT)]
/// 
/// - Vấn đề giải quyết:
///   Luồng cập nhật thiết bị có những bất biến (invariants) riêng biệt so với luồng tạo mới:
///   1. Không cho phép đổi mã thiết bị (`Code`) sau khi đã tạo.
///   2. Cho phép chuyển trạng thái sang `Retired` (Ngưng sử dụng), NHƯNG phải chặn nếu thiết bị đang có
///      phiếu bảo trì mở (`hasOpenTicket == true`).
/// 
/// - Cách giải quyết:
///   Đóng gói toàn bộ logic kiểm tra và cập nhật vào `UpdateEquipmentHandler`:
///   - Tham số `hasOpenTicket` được truyền từ tầng điều phối (qua contract của Maintenance) để module Equipment
///     không phải truy vấn trực tiếp bảng `MaintenanceTickets`, giữ sạch ranh giới module.
///   - Mọi thay đổi thuộc tính được thực hiện trên thực thể Tracked của EF Core và lưu nguyên tử qua `SaveChangesAsync`.
/// </summary>
public sealed class UpdateEquipmentHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory,
    GetEquipmentByIdHandler getEquipmentByIdHandler)
{
    public async Task<ServiceResult<EquipmentResponse>> HandleAsync(
        int id,
        UpdateEquipmentRequest request,
        bool hasOpenTicket,
        CancellationToken cancellationToken = default)
    {
        var equipment = await db.Equipment.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (equipment is null)
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status404NotFound, "Equipment Not Found");

        var code = request.Code.Trim();
        if (!await EquipmentProjection.DepartmentExistsAsync(organizationDirectory, request.DepartmentId, cancellationToken))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status404NotFound, "Department does not exist");

        // Cập nhật phải xác minh ID phòng bảo trì giống lúc tạo, để lỗi đầu vào có message rõ
        // và không bị phụ thuộc vào việc SQL ném DbUpdateException từ FK liên module.
        if (request.MaintenanceDepartmentId.HasValue &&
            !await EquipmentProjection.DepartmentExistsAsync(organizationDirectory, request.MaintenanceDepartmentId.Value, cancellationToken))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status404NotFound,
                "Maintenance department does not exist");

        // Giữ cùng quy tắc khi sửa để không thể tạo thiết bị hợp lệ rồi chuyển queue bảo trì
        // sang phòng ban thông thường ở lần cập nhật sau.
        if (request.MaintenanceDepartmentId.HasValue &&
            !await organizationDirectory.IsMaintenanceTeamAsync(request.MaintenanceDepartmentId.Value, cancellationToken))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status400BadRequest,
                "Maintenance department must be a maintenance team");

        if (equipment.Code != code)
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status400BadRequest,
                "Equipment code cannot be changed after creation");

        var status = string.IsNullOrWhiteSpace(request.Status) ? EquipmentStatuses.Active : request.Status.Trim();
        if (status is not (EquipmentStatuses.Active or EquipmentStatuses.Inactive or EquipmentStatuses.Retired))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status400BadRequest, "Invalid equipment status");

        // Kết quả ticket được API điều phối qua contract; Equipment không tự truy vấn bảng của Maintenance.
        if (status == EquipmentStatuses.Retired && hasOpenTicket)
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status400BadRequest,
                "Cannot retire equipment while it has open maintenance tickets");

        if (request.PurchasedDate.HasValue && request.PurchasedDate.Value.Date > DateTime.UtcNow.Date)
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status400BadRequest,
                "Purchased date cannot be in the future");

        equipment.Name = request.Name.Trim();
        equipment.DepartmentId = request.DepartmentId;
        equipment.MaintenanceDepartmentId = request.MaintenanceDepartmentId;
        equipment.Status = status;
        equipment.PurchasedDate = request.PurchasedDate;
        equipment.Description = request.Description?.Trim();
        equipment.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var response = await getEquipmentByIdHandler.HandleAsync(id, cancellationToken);
        return ServiceResult<EquipmentResponse>.Success(response!);
    }
}

