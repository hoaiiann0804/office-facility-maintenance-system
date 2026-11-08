using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.UpdateUser;

/// <summary>
/// [MINIMAL API ENDPOINT - CẬP NHẬT THÔNG TIN NGƯỜI DÙNG]
///
/// - Luồng đi (Request Flow):
///   HTTP PUT /api/users/{id:int}
///     -> UpdateUserEndpoint.MapUpdateUserEndpoint(...)
///     -> IUserAdministrationService.UpdateAsync(...) (điều phối xuống UpdateUserHandler)
///     -> Trả về HTTP 200 OK hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class UpdateUserEndpoint
{
    public static RouteHandlerBuilder MapUpdateUserEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPut("{id:int}", async (
            int id,
            [FromBody] UpdateUserRequest request,
            IUserAdministrationService userAdministration,
            CancellationToken cancellationToken) =>
        {
            var result = await userAdministration.UpdateAsync(id, request, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.Ok(result.Data);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("UpdateUser")
        .Produces<UserResponse>(StatusCodes.Status200OK);
    }
}

