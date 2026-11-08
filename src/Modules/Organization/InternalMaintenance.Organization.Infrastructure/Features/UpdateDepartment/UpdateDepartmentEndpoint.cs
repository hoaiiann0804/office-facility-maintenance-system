using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Organization.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Organization.Infrastructure.Features.UpdateDepartment;

/// <summary>
/// [MINIMAL API ENDPOINT - CẬP NHẬT PHÒNG BAN]
///
/// - Luồng đi (Request Flow):
///   HTTP PUT /api/departments/{id:int}
///     -> UpdateDepartmentEndpoint.MapUpdateDepartmentEndpoint(...)
///     -> IDepartmentCatalog.UpdateAsync(...) (điều phối xuống UpdateDepartmentHandler)
///     -> Trả về HTTP 200 OK hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class UpdateDepartmentEndpoint
{
    public static RouteHandlerBuilder MapUpdateDepartmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPut("{id:int}", async (
            int id,
            [FromBody] UpdateDepartmentRequest request,
            IDepartmentCatalog catalog,
            CancellationToken cancellationToken) =>
        {
            var result = await catalog.UpdateAsync(id, request, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.Ok(result.Data);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("UpdateDepartment")
        .Produces<DepartmentResponse>(StatusCodes.Status200OK);
    }
}

