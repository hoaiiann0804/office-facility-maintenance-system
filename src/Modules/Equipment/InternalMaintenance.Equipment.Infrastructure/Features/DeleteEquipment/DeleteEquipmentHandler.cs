using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Assets.Infrastructure.Features.DeleteEquipment;

/// <summary>
/// [VERTICAL SLICE - FEATURE: XÓA THIẾT BỊ (DELETE EQUIPMENT)]
/// 
/// - Vấn đề giải quyết:
///   Thiết bị đã từng phát sinh lịch sử bảo trì (MaintenanceTicket) thì không được phép xóa vật lý khỏi DB
///   để bảo toàn dữ liệu kiểm toán (Audit Trail) và tránh lỗi khóa ngoại (Foreign Key Constraint).
/// 
/// - Cách giải quyết:
///   Tách riêng `DeleteEquipmentHandler` chỉ tập trung kiểm tra sự tồn tại của thiết bị và cờ `hasAnyTicket`
///   trước khi xóa khỏi `AppDbContext`.
/// </summary>
public sealed class DeleteEquipmentHandler(AppDbContext db)
{
    public async Task<ServiceResult> HandleAsync(
        int id,
        bool hasAnyTicket,
        CancellationToken cancellationToken = default)
    {
        var equipment = await db.Equipment.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (equipment is null)
            return ServiceResult.Fail(StatusCodes.Status404NotFound, "Equipment Not Found");

        // API hỏi Maintenance qua contract trước khi gọi; Equipment chỉ xóa entity sở hữu của mình.
        if (hasAnyTicket)
            return ServiceResult.Fail(StatusCodes.Status400BadRequest,
                "Cannot delete equipment because it is related to maintenance tickets");

        db.Equipment.Remove(equipment);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }
}

