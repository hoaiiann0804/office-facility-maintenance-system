using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.GetUserById;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY CHI TIẾT NGƯỜI DÙNG THEO ID]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/users/{id:int}
///     -> GetUserByIdEndpoint.MapGetUserByIdEndpoint(...)
///     -> IUserAdministrationService.GetByIdAsync(...) (điều phối xuống GetUserByIdHandler)
///     -> Trả về HTTP 200 OK hoặc 404 NotFound.
/// </summary>
public static class GetUserByIdEndpoint
{
    public static RouteHandlerBuilder MapGetUserByIdEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("{id:int}", async (
            int id,
            IUserAdministrationService userAdministration,
            CancellationToken cancellationToken) =>
        {
            var user = await userAdministration.GetByIdAsync(id, cancellationToken);
            return user is null
                ? Results.NotFound(new { message = "User not found" })
                : Results.Ok(user);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("GetUserById")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

