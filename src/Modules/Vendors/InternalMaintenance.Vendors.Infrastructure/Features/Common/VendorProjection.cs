using System.Linq.Expressions;
using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Domain;

namespace InternalMaintenance.Vendors.Infrastructure.Features.Common;

/// <summary>
/// Ánh xạ dùng chung nội bộ trong module Vendors (Shared Mapping & SQL Projection).
/// 
/// [LUỒNG ĐI & CÁCH GIẢI QUYẾT VẤN ĐỀ]:
/// 1. Vấn đề khi tái cấu trúc:
///    - Các luồng Đọc (`GetVendors`, `GetVendorById`) cần một `Expression<Func<Vendor, VendorResponse>>`
///      để EF Core dịch trực tiếp sang câu lệnh `SELECT` dưới SQL Server.
///    - Các luồng Ghi (`CreateVendor`, `UpdateVendor`, `ToggleVendorActive`) sau khi `SaveChangesAsync()`
///      đã có sẵn đối tượng `Vendor` trên RAM (không có trường thông tin phụ thuộc bảng khác), nên chỉ cần
///      hàm `ToResponse(Vendor)` thuần trên RAM để đỡ tốn thêm 1 câu lệnh `SELECT` thừa xuống Database.
/// 2. Giải pháp:
///    Gom cả `ToResponseProjection()` (dùng cho IQueryable SQL) và `ToResponse(Vendor)` (dùng trên RAM sau khi ghi)
///    vào class `internal static VendorProjection` tại `Features/Common`.
/// </summary>
internal static class VendorProjection
{
    /// <summary>
    /// Biểu thức Expression Tree để EF Core dịch sang câu lệnh SQL SELECT cho các Query Handlers.
    /// </summary>
    public static Expression<Func<Vendor, VendorResponse>> ToResponseProjection() => vendor => new VendorResponse
    {
        Id = vendor.Id,
        Name = vendor.Name,
        ContactPerson = vendor.ContactPerson,
        Phone = vendor.Phone,
        Email = vendor.Email,
        Address = vendor.Address,
        IsActive = vendor.IsActive,
        CreatedAt = vendor.CreatedAt
    };

    /// <summary>
    /// Chuyển đổi trực tiếp thực thể Vendor trên RAM sang VendorResponse sau khi Create/Update/ToggleActive,
    /// tiết kiệm 1 vòng truy vấn (Database Round-trip) vì Vendor không phụ thuộc dữ liệu từ module khác.
    /// </summary>
    public static VendorResponse ToResponse(Vendor vendor) => new()
    {
        Id = vendor.Id,
        Name = vendor.Name,
        ContactPerson = vendor.ContactPerson,
        Phone = vendor.Phone,
        Email = vendor.Email,
        Address = vendor.Address,
        IsActive = vendor.IsActive,
        CreatedAt = vendor.CreatedAt
    };
}

