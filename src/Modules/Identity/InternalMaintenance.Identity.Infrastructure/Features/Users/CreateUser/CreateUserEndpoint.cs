using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.CreateUser;

/// <summary>
/// [MINIMAL API ENDPOINT - TẠO MỚI TÀI KHOẢN NGƯỜI DÙNG]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/users
///     -> CreateUserEndpoint.MapCreateUserEndpoint(...)
///     -> IUserAdministrationService.CreateAsync(...) (điều phối xuống CreateUserHandler)
///     -> Trả về HTTP 201 CreatedAtRoute("GetUsers") hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class CreateUserEndpoint
{
    public static RouteHandlerBuilder MapCreateUserEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("", async (
            [FromBody] CreateUserRequest request,
            IUserAdministrationService userAdministration,
            CancellationToken cancellationToken) =>
        {
            var result = await userAdministration.CreateAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.CreatedAtRoute("GetUsers", new { id = result.Data!.Id }, result.Data);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("CreateUser")
        .Produces<UserResponse>(StatusCodes.Status201Created);
    }
}

