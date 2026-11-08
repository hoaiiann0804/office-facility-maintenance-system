using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Vendors.Infrastructure.Features.UpdateVendor;

/// <summary>
/// [VERTICAL SLICE - FEATURE: CẬP NHẬT THÔNG TIN ĐỐI TÁC (UPDATE VENDOR)]
/// 
/// - Luồng đi (Request Flow):
///   `VendorsController.UpdateVendor(id, request)` -> `IVendorCatalog.UpdateAsync` -> `UpdateVendorHandler.HandleAsync`
///   -> Tìm Vendor theo `id` (Tracked) -> Kiểm tra trùng tên loại trừ chính nó (`item.Id != id`)
///   -> Cập nhật thuộc tính -> `SaveChangesAsync()` -> `ServiceResult<VendorResponse> (200 OK)`.
/// 
/// - Cách giải quyết vấn đề khi tái cấu trúc:
///   Tách riêng quy tắc kiểm tra trùng tên khi cập nhật (`item.Name == name && item.Id != id`) khỏi quy tắc tạo mới,
///   đảm bảo đối tác có thể giữ nguyên tên cũ khi chỉ sửa số điện thoại/địa chỉ mà không bị báo lỗi trùng tên.
/// </summary>
public sealed class UpdateVendorHandler(AppDbContext db)
{
    public async Task<ServiceResult<VendorResponse>> HandleAsync(
        int id,
        UpdateVendorRequest request,
        CancellationToken cancellationToken = default)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (vendor is null)
            return ServiceResult<VendorResponse>.Fail(StatusCodes.Status404NotFound, "Vendor not found");

        var name = request.Name.Trim();
        if (await db.Vendors.AnyAsync(item => item.Name == name && item.Id != id, cancellationToken))
            return ServiceResult<VendorResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "A vendor with this name already exists");

        vendor.Name = name;
        vendor.ContactPerson = request.ContactPerson?.Trim();
        vendor.Phone = request.Phone?.Trim();
        vendor.Email = request.Email?.Trim();
        vendor.Address = request.Address?.Trim();
        vendor.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<VendorResponse>.Success(VendorProjection.ToResponse(vendor));
    }
}

