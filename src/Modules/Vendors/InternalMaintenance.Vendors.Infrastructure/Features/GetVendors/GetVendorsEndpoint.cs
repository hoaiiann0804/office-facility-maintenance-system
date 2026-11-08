using InternalMaintenance.Vendors.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Vendors.Infrastructure.Features.GetVendors;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY DANH SÁCH VENDOR]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/vendors?isActive=...
///     -> GetVendorsEndpoint.MapGetVendorsEndpoint(...)
///     -> IVendorCatalog.GetAllAsync(...) (điều phối xuống GetVendorsHandler)
///     -> Trả về HTTP 200 OK kèm danh sách VendorResponse.
/// - Giải quyết vấn đề:
///   Đặt Endpoint ngay trong thư mục `Features/GetVendors/` cạnh `GetVendorsHandler.cs`
///   đúng chuẩn Vertical Slice của FullStackHero, giúp gom toàn bộ HTTP route và logic đọc danh sách vào một chỗ.
/// </summary>
public static class GetVendorsEndpoint
{
    public static RouteHandlerBuilder MapGetVendorsEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("", async (
            [FromQuery] bool? isActive,
            IVendorCatalog vendorCatalog,
            CancellationToken cancellationToken) =>
        {
            var vendors = await vendorCatalog.GetAllAsync(isActive, cancellationToken);
            return Results.Ok(vendors);
        })
        .AllowAnonymous()
        .WithName("GetVendors")
        .Produces<IReadOnlyList<VendorResponse>>(StatusCodes.Status200OK);
    }
}

