using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Domain;
using InternalMaintenance.Vendors.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Vendors.Infrastructure.Features.CreateVendor;

/// <summary>
/// [VERTICAL SLICE - FEATURE: TẠO MỚI ĐỐI TÁC SỬA CHỮA (CREATE VENDOR)]
/// 
/// - Luồng đi (Request Flow):
///   `VendorsController.CreateVendor(request)` -> `IVendorCatalog.CreateAsync` -> `CreateVendorHandler.HandleAsync`
///   -> Kiểm tra trùng tên (`AnyAsync`) -> `db.Vendors.Add(vendor)` -> `SaveChangesAsync()` -> `ServiceResult<VendorResponse> (201 Created)`.
/// 
/// - Cách giải quyết vấn đề khi tái cấu trúc:
///   1. Chuẩn hóa dữ liệu đầu vào (`Trim()` tên, người liên hệ, số điện thoại, email, địa chỉ) trước khi kiểm tra và lưu.
///   2. Đảm bảo tính duy nhất của tên đối tác (`Name`) ở tầng ứng dụng để trả về mã lỗi `400 Bad Request` rõ ràng.
///   3. Khởi tạo mặc định `IsActive = true` và `CreatedAt = DateTime.UtcNow`, sau khi lưu xong ánh xạ trực tiếp
///      trên RAM qua `VendorProjection.ToResponse(vendor)` mà không cần query lại DB.
/// </summary>
public sealed class CreateVendorHandler(AppDbContext db)
{
    public async Task<ServiceResult<VendorResponse>> HandleAsync(
        CreateVendorRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await db.Vendors.AnyAsync(vendor => vendor.Name == name, cancellationToken))
            return ServiceResult<VendorResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "A vendor with this name already exists");

        var vendor = new Vendor
        {
            Name = name,
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<VendorResponse>.Success(
            VendorProjection.ToResponse(vendor),
            StatusCodes.Status201Created);
    }
}

