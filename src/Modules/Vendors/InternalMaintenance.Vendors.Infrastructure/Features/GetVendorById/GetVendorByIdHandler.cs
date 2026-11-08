using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Infrastructure.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Vendors.Infrastructure.Features.GetVendorById;

/// <summary>
/// [VERTICAL SLICE - FEATURE: LẤY CHI TIẾT ĐỐI TÁC THEO ID (GET VENDOR BY ID)]
/// 
/// - Luồng đi (Request Flow):
///   `VendorsController.GetVendorById(id)` -> `IVendorCatalog.GetByIdAsync` -> `GetVendorByIdHandler.HandleAsync`
///   -> `AppDbContext.Vendors.AsNoTracking().Where(v => v.Id == id)` -> SQL Server -> `VendorResponse?`.
/// 
/// - Cách giải quyết vấn đề khi tái cấu trúc:
///   Cô lập truy vấn lấy đơn lẻ 1 đối tác theo khóa chính với `AsNoTracking()` và chiếu thẳng sang `VendorResponse`.
/// </summary>
public sealed class GetVendorByIdHandler(AppDbContext db)
{
    public Task<VendorResponse?> HandleAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        db.Vendors
            .AsNoTracking()
            .Where(vendor => vendor.Id == id)
            .Select(VendorProjection.ToResponseProjection())
            .FirstOrDefaultAsync(cancellationToken);
}

