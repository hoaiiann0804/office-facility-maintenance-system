using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Assets.Infrastructure.Features.Common;
using InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentById;
using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = InternalMaintenance.Assets.Domain.Equipment;

namespace InternalMaintenance.Assets.Infrastructure.Features.CreateEquipment;

/// <summary>
/// [VERTICAL SLICE - FEATURE: TẠO MỚI THIẾT BỊ (CREATE EQUIPMENT)]
/// 
/// - Vấn đề giải quyết:
///   Khi tạo mới thiết bị, hệ thống cần kiểm tra nhiều quy tắc liên module (phòng ban tồn tại, phòng bảo trì
///   phải là Maintenance Team) và quy tắc nội bộ (mã Code duy nhất, ngày mua không ở tương lai, trạng thái khởi tạo hợp lệ).
///   Nếu để chung trong một Service lớn, các quy tắc của Create dễ bị lẫn lộn với quy tắc của Update (ví dụ: Create chỉ cho phép
///   Active/Inactive, còn Update cho phép thêm Retired).
/// 
/// - Cách giải quyết:
///   Cô lập toàn bộ quy tắc nghiệp vụ của hành động Tạo mới vào `CreateEquipmentHandler`:
///   1. Xác thực `DepartmentId` và `MaintenanceDepartmentId` qua `IOrganizationDirectory` để trả lỗi 400/404 thân thiện,
///      tránh rơi xuống lỗi SQL Foreign Key 500.
///   2. Đảm bảo `MaintenanceDepartmentId` thực sự được đánh dấu `IsMaintenanceTeam = true` để ticket sau này không bị đẩy nhầm queue.
///   3. Kiểm tra trùng mã `Code` và ngày mua `PurchasedDate`.
///   4. Lưu thực thể vào `AppDbContext` và dùng `GetEquipmentByIdHandler` trả về DTO hoàn chỉnh kèm tên phòng ban.
/// </summary>
public sealed class CreateEquipmentHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory,
    GetEquipmentByIdHandler getEquipmentByIdHandler)
{
    public async Task<ServiceResult<EquipmentResponse>> HandleAsync(
        CreateEquipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();
        var name = request.Name.Trim();

        if (!await EquipmentProjection.DepartmentExistsAsync(organizationDirectory, request.DepartmentId, cancellationToken))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status404NotFound, "Department does not exist");

        // Kiểm tra cả phòng ban phụ trách bảo trì qua Organization contract; nếu chỉ kiểm tra
        // DepartmentId thì MaintenanceDepartmentId sai sẽ rơi xuống FK và thành lỗi SQL 500.
        if (request.MaintenanceDepartmentId.HasValue &&
            !await EquipmentProjection.DepartmentExistsAsync(organizationDirectory, request.MaintenanceDepartmentId.Value, cancellationToken))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status404NotFound,
                "Maintenance department does not exist");

        // Có Department chưa đủ: trường này có nghĩa là nơi nhận việc bảo trì, nên phải
        // được Organization đánh dấu IsMaintenanceTeam để ticket không vào queue sai nhóm.
        if (request.MaintenanceDepartmentId.HasValue &&
            !await organizationDirectory.IsMaintenanceTeamAsync(request.MaintenanceDepartmentId.Value, cancellationToken))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status400BadRequest,
                "Maintenance department must be a maintenance team");

        if (await db.Equipment.AnyAsync(equipment => equipment.Code == code, cancellationToken))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status404NotFound, "Equipment code already exists");

        if (request.PurchasedDate.HasValue && request.PurchasedDate.Value.Date > DateTime.UtcNow.Date)
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status400BadRequest,
                "Purchased date cannot be in the future");

        var status = string.IsNullOrWhiteSpace(request.Status) ? EquipmentStatuses.Active : request.Status.Trim();
        if (status is not (EquipmentStatuses.Active or EquipmentStatuses.Inactive))
            return ServiceResult<EquipmentResponse>.Fail(StatusCodes.Status400BadRequest, "Invalid equipment status");

        var equipment = new EquipmentEntity
        {
            Code = code,
            Name = name,
            DepartmentId = request.DepartmentId,
            MaintenanceDepartmentId = request.MaintenanceDepartmentId,
            Status = status,
            PurchasedDate = request.PurchasedDate,
            Description = request.Description?.Trim()
        };

        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(cancellationToken);

        var response = await getEquipmentByIdHandler.HandleAsync(equipment.Id, cancellationToken);
        return ServiceResult<EquipmentResponse>.Success(response!, StatusCodes.Status201Created);
    }
}

