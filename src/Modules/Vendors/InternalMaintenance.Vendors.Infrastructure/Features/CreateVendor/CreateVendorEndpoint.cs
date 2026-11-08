using InternalMaintenance.Vendors.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Vendors.Infrastructure.Features.CreateVendor;

/// <summary>
/// [MINIMAL API ENDPOINT - TẠO MỚI VENDOR]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/vendors
///     -> CreateVendorEndpoint.MapCreateVendorEndpoint(...)
///     -> IVendorCatalog.CreateAsync(...) (điều phối xuống CreateVendorHandler)
///     -> Trả về HTTP 201 CreatedAtRoute("GetVendorById") hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class CreateVendorEndpoint
{
    public static RouteHandlerBuilder MapCreateVendorEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("", async (
            [FromBody] CreateVendorRequest request,
            IVendorCatalog vendorCatalog,
            CancellationToken cancellationToken) =>
        {
            var result = await vendorCatalog.CreateAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.CreatedAtRoute("GetVendorById", new { id = result.Data!.Id }, result.Data);
        })
        .WithName("CreateVendor")
        .Produces<VendorResponse>(StatusCodes.Status201Created);
    }
}

