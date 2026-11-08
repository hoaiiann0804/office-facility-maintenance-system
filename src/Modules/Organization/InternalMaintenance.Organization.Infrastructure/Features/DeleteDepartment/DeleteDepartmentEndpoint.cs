using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Organization.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Organization.Infrastructure.Features.DeleteDepartment;

/// <summary>
/// [MINIMAL API ENDPOINT - XÓA PHÒNG BAN]
///
/// - Luồng đi (Request Flow):
///   HTTP DELETE /api/departments/{id:int}
///     -> DeleteDepartmentEndpoint.MapDeleteDepartmentEndpoint(...)
///     -> IDepartmentCatalog.DeleteAsync(...) (điều phối xuống DeleteDepartmentHandler)
///     -> Trả về HTTP 204 NoContent hoặc mã lỗi từ ServiceResult (404 / 409 Conflict).
/// </summary>
public static class DeleteDepartmentEndpoint
{
    public static RouteHandlerBuilder MapDeleteDepartmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapDelete("{id:int}", async (
            int id,
            IDepartmentCatalog catalog,
            CancellationToken cancellationToken) =>
        {
            var result = await catalog.DeleteAsync(id, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.NoContent();
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("DeleteDepartment")
        .Produces(StatusCodes.Status204NoContent);
    }
}

