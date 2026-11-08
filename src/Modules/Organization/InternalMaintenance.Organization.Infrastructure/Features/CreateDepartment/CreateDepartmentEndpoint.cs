using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Organization.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Organization.Infrastructure.Features.CreateDepartment;

/// <summary>
/// [MINIMAL API ENDPOINT - TẠO MỚI PHÒNG BAN]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/departments
///     -> CreateDepartmentEndpoint.MapCreateDepartmentEndpoint(...)
///     -> IDepartmentCatalog.CreateAsync(...) (điều phối xuống CreateDepartmentHandler)
///     -> Trả về HTTP 201 CreatedAtRoute("GetDepartmentById") hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class CreateDepartmentEndpoint
{
    public static RouteHandlerBuilder MapCreateDepartmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("", async (
            [FromBody] CreateDepartmentRequest request,
            IDepartmentCatalog catalog,
            CancellationToken cancellationToken) =>
        {
            var result = await catalog.CreateAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.CreatedAtRoute("GetDepartmentById", new { id = result.Data!.Id }, result.Data);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("CreateDepartment")
        .Produces<DepartmentResponse>(StatusCodes.Status201Created);
    }
}

