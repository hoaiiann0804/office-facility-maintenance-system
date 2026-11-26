using InternalMaintenance.Vendors.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Vendors.Infrastructure.Features.ToggleVendorActive;

/// <summary>
/// [MINIMAL API ENDPOINT - BẬT/TẮT TRẠNG THÁI HOẠT ĐỘNG CỦA VENDOR]
///
/// - Luồng đi (Request Flow):
///   HTTP PATCH /api/vendors/{id:int}/toggle-active
///     -> ToggleVendorActiveEndpoint.MapToggleVendorActiveEndpoint(...)
///     -> IVendorCatalog.ToggleActiveAsync(...) (điều phối xuống ToggleVendorActiveHandler)
///     -> Trả về HTTP 200 OK hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class ToggleVendorActiveEndpoint
{
    public static RouteHandlerBuilder MapToggleVendorActiveEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPatch("{id:int}/toggle-active", async (
            int id,
            IVendorCatalog vendorCatalog,
            CancellationToken cancellationToken) =>
        {
            var result = await vendorCatalog.ToggleActiveAsync(id, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.Ok(result.Data);
        })
        // [FIX G-05]: Chỉ Admin và Manager mới có quyền bật/tắt trạng thái hoạt động của đối tác.
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Manager" })
        .WithName("ToggleVendorActive")
        .Produces<VendorResponse>(StatusCodes.Status200OK);
    }
}

