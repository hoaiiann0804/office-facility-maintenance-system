using InternalMaintenance.Vendors.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Vendors.Infrastructure.Features.UpdateVendor;

/// <summary>
/// [MINIMAL API ENDPOINT - CẬP NHẬT VENDOR]
///
/// - Luồng đi (Request Flow):
///   HTTP PUT /api/vendors/{id:int}
///     -> UpdateVendorEndpoint.MapUpdateVendorEndpoint(...)
///     -> IVendorCatalog.UpdateAsync(...) (điều phối xuống UpdateVendorHandler)
///     -> Trả về HTTP 200 OK hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class UpdateVendorEndpoint
{
    public static RouteHandlerBuilder MapUpdateVendorEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPut("{id:int}", async (
            int id,
            [FromBody] UpdateVendorRequest request,
            IVendorCatalog vendorCatalog,
            CancellationToken cancellationToken) =>
        {
            var result = await vendorCatalog.UpdateAsync(id, request, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.Ok(result.Data);
        })
        .WithName("UpdateVendor")
        .Produces<VendorResponse>(StatusCodes.Status200OK);
    }
}

