using InternalMaintenance.Vendors.Infrastructure.Features.CreateVendor;
using InternalMaintenance.Vendors.Infrastructure.Features.GetVendorById;
using InternalMaintenance.Vendors.Infrastructure.Features.GetVendors;
using InternalMaintenance.Vendors.Infrastructure.Features.ToggleVendorActive;
using InternalMaintenance.Vendors.Infrastructure.Features.UpdateVendor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Vendors.Infrastructure.DependencyInjection;

/// <summary>
/// [MODULE ENDPOINT REGISTRATION - VENDORS]
/// Gom toàn bộ 5 Minimal API Endpoints của các Vertical Slices trong module Vendors
/// vào chung một Route Group `/api/vendors` theo chuẩn FullStackHero.
/// </summary>
public static class VendorEndpoints
{
    public static IEndpointRouteBuilder MapVendorEndpoints(this IEndpointRouteBuilder app)
    {
        // [FIX G-05 - BẢO MẬT DANH BẠ ĐỐI TÁC]:
        // Yêu cầu xác thực JWT ở cấp group chung cho toàn bộ /api/vendors (để cả Admin, Manager, Technician
        // đều đọc được danh sách Vendor khi điều phối thiết bị, nhưng chặn truy cập nặc danh từ bên ngoài).
        // Các thao tác thêm/sửa/bật-tắt Vendor bên dưới yêu cầu riêng quyền Admin hoặc Manager.
        var group = app.MapGroup("api/vendors")
            .WithTags("Vendors")
            .RequireAuthorization();

        group.MapGetVendorsEndpoint();
        group.MapGetVendorByIdEndpoint();
        group.MapCreateVendorEndpoint();
        group.MapUpdateVendorEndpoint();
        group.MapToggleVendorActiveEndpoint();

        return app;
    }
}

