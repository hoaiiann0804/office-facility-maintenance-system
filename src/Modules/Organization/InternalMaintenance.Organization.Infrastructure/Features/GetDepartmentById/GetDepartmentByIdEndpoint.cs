using InternalMaintenance.Organization.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentById;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY CHI TIẾT PHÒNG BAN THEO ID]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/departments/{id:int}
///     -> GetDepartmentByIdEndpoint.MapGetDepartmentByIdEndpoint(...)
///     -> IDepartmentCatalog.GetByIdAsync(...) (điều phối xuống GetDepartmentByIdHandler)
///     -> Trả về HTTP 200 OK hoặc 404 NotFound.
/// </summary>
public static class GetDepartmentByIdEndpoint
{
    public static RouteHandlerBuilder MapGetDepartmentByIdEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("{id:int}", async (
            int id,
            IDepartmentCatalog catalog,
            CancellationToken cancellationToken) =>
        {
            var department = await catalog.GetByIdAsync(id, cancellationToken);
            return department is null
                ? Results.NotFound(new { message = "Department not found" })
                : Results.Ok(department);
        })
        .RequireAuthorization()
        .WithName("GetDepartmentById")
        .Produces<DepartmentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

