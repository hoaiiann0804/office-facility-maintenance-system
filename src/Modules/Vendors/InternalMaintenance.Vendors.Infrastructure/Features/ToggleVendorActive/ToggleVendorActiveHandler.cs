using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Vendors.Infrastructure.Features.ToggleVendorActive;

/// <summary>
/// [VERTICAL SLICE - FEATURE: BẬT/TẮT TRẠNG THÁI HOẠT ĐỘNG CỦA ĐỐI TÁC (TOGGLE VENDOR ACTIVE)]
/// 
/// - Luồng đi (Request Flow):
///   `VendorsController.ToggleVendorActive(id)` -> `IVendorCatalog.ToggleActiveAsync` -> `ToggleVendorActiveHandler.HandleAsync`
///   -> Tìm Vendor theo `id` (Tracked) -> Đảo cờ `vendor.IsActive = !vendor.IsActive` -> `SaveChangesAsync()`.
/// 
/// - Cách giải quyết vấn đề khi tái cấu trúc:
///   Hệ thống không cho phép xóa vật lý đối tác (vì `TicketVendorLogs` và báo cáo chi phí bảo trì cần giữ lịch sử đối tác).
///   Do đó, hành động vô hiệu hóa/kích hoạt lại đối tác được tách thành 1 Feature Slice riêng (`ToggleVendorActiveHandler`).
///   Khi `IsActive = false`, `IVendorDirectory.IsActiveAsync` sẽ chặn không cho gửi thêm ticket mới sang đối tác này,
///   nhưng các báo cáo lịch sử vẫn tra cứu được tên đối tác bình thường.
/// </summary>
public sealed class ToggleVendorActiveHandler(AppDbContext db)
{
    public async Task<ServiceResult<VendorResponse>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (vendor is null)
            return ServiceResult<VendorResponse>.Fail(StatusCodes.Status404NotFound, "Vendor not found");

        vendor.IsActive = !vendor.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<VendorResponse>.Success(VendorProjection.ToResponse(vendor));
    }
}

