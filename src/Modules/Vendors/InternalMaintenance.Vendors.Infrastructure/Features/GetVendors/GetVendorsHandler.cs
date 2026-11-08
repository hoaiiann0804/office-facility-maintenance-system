using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Infrastructure.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Vendors.Infrastructure.Features.GetVendors;

/// <summary>
/// [VERTICAL SLICE - FEATURE: LẤY DANH SÁCH ĐỐI TÁC SỬA CHỮA (GET VENDORS)]
/// 
/// - Luồng đi (Request Flow):
///   `VendorsController.GetVendors(isActive)` -> `IVendorCatalog.GetAllAsync` -> `GetVendorsHandler.HandleAsync`
///   -> `AppDbContext.Vendors.AsNoTracking()` -> SQL Server -> `IReadOnlyList<VendorResponse>`.
/// 
/// - Cách giải quyết vấn đề khi tái cấu trúc:
///   Tách riêng truy vấn danh sách Vendor khỏi các luồng ghi dữ liệu:
///   1. Dùng `AsNoTracking()` vì đây là truy vấn chỉ đọc (Read-only), giúp giảm tải cho ChangeTracker của EF Core.
///   2. Nếu màn hình chọn Vendor khi gửi thiết bị đi sửa truyền `isActive = true`, bộ lọc `WHERE IsActive = 1`
///      được áp dụng ngay dưới SQL. Nếu màn hình quản trị truyền `isActive = null`, trả về toàn bộ danh sách.
///   3. Sắp xếp theo tên chữ cái (`OrderBy(v => v.Name)`) để UI hiển thị dropdown đồng nhất.
/// </summary>
public sealed class GetVendorsHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<VendorResponse>> HandleAsync(
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = db.Vendors.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(vendor => vendor.IsActive == isActive.Value);

        return await query
            .OrderBy(vendor => vendor.Name)
            .Select(VendorProjection.ToResponseProjection())
            .ToListAsync(cancellationToken);
    }
}

