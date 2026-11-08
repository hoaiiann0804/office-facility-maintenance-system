using System.Security.Claims;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.BuildingBlocks.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentPage;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY DANH SÁCH THIẾT BỊ CÓ PHÂN TRANG]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/equipment?page=...&amp;pageSize=...&amp;keyword=...&amp;departmentId=...&amp;status=...
///     -> GetEquipmentPageEndpoint.MapGetEquipmentPageEndpoint(...)
///     -> Trích xuất Role và DepartmentId từ ClaimsPrincipal (không phụ thuộc ngược vào Host)
///     -> IEquipmentCatalog.GetPageAsync(...) (điều phối xuống GetEquipmentPageHandler)
///     -> Trả về HTTP 200 OK kèm PagedResponse&lt;EquipmentResponse&gt;.
/// </summary>
public static class GetEquipmentPageEndpoint
{
    public static RouteHandlerBuilder MapGetEquipmentPageEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("", async (
            [AsParameters] EquipmentQuery query,
            ClaimsPrincipal user,
            IEquipmentCatalog catalog,
            CancellationToken cancellationToken) =>
        {
            var isAdmin = user.GetRole() == "Admin";
            var departmentId = user.GetDepartmentId();
            var result = await catalog.GetPageAsync(query, isAdmin, departmentId, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("GetEquipment")
        .Produces<PagedResponse<EquipmentResponse>>(StatusCodes.Status200OK);
    }
}

