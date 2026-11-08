using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Organization.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentsPage;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY DANH SÁCH PHÒNG BAN CÓ PHÂN TRANG]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/departments?page=...&amp;pageSize=...&amp;keyword=...&amp;isMaintenanceTeam=...
///     -> GetDepartmentsPageEndpoint.MapGetDepartmentsPageEndpoint(...)
///     -> IDepartmentCatalog.GetPageAsync(...) (điều phối xuống GetDepartmentsPageHandler)
///     -> Trả về HTTP 200 OK kèm PagedResponse&lt;DepartmentResponse&gt;.
/// </summary>
public static class GetDepartmentsPageEndpoint
{
    public static RouteHandlerBuilder MapGetDepartmentsPageEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("", async (
            [AsParameters] DepartmentQuery query,
            IDepartmentCatalog catalog,
            CancellationToken cancellationToken) =>
        {
            var departments = await catalog.GetPageAsync(query, cancellationToken);
            return Results.Ok(departments);
        })
        .RequireAuthorization()
        .WithName("GetDepartments")
        .Produces<PagedResponse<DepartmentResponse>>(StatusCodes.Status200OK);
    }
}

