using InternalMaintenance.Vendors.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Vendors.Infrastructure.Features.GetVendorById;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY CHI TIẾT VENDOR THEO ID]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/vendors/{id:int}
///     -> GetVendorByIdEndpoint.MapGetVendorByIdEndpoint(...)
///     -> IVendorCatalog.GetByIdAsync(...) (điều phối xuống GetVendorByIdHandler)
///     -> Trả về HTTP 200 OK hoặc 404 NotFound.
/// </summary>
public static class GetVendorByIdEndpoint
{
    public static RouteHandlerBuilder MapGetVendorByIdEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("{id:int}", async (
            int id,
            IVendorCatalog vendorCatalog,
            CancellationToken cancellationToken) =>
        {
            var vendor = await vendorCatalog.GetByIdAsync(id, cancellationToken);
            return vendor is null
                ? Results.NotFound(new { message = "Vendor not found" })
                : Results.Ok(vendor);
        })
        .AllowAnonymous()
        .WithName("GetVendorById")
        .Produces<VendorResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

