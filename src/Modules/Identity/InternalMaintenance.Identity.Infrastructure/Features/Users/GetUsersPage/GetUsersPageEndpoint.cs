using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.GetUsersPage;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY DANH SÁCH NGƯỜI DÙNG CÓ PHÂN TRANG]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/users?page=...&amp;pageSize=...&amp;keyword=...&amp;role=...&amp;departmentId=...&amp;isActive=...
///     -> GetUsersPageEndpoint.MapGetUsersPageEndpoint(...)
///     -> IUserAdministrationService.GetPageAsync(...) (điều phối xuống GetUsersPageHandler)
///     -> Trả về HTTP 200 OK kèm PagedResponse&lt;UserResponse&gt;.
/// </summary>
public static class GetUsersPageEndpoint
{
    public static RouteHandlerBuilder MapGetUsersPageEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("", async (
            [AsParameters] UserQuery query,
            IUserAdministrationService userAdministration,
            CancellationToken cancellationToken) =>
        {
            var users = await userAdministration.GetPageAsync(query, cancellationToken);
            return Results.Ok(users);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{UserRoles.Admin},{UserRoles.Manager}" })
        .WithName("GetUsers")
        .Produces<PagedResponse<UserResponse>>(StatusCodes.Status200OK);
    }
}

