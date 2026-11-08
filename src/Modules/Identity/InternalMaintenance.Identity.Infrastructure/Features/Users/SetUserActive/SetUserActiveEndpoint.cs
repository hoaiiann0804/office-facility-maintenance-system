using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.SetUserActive;

/// <summary>
/// [MINIMAL API ENDPOINT - KÍCH HOẠT / VÔ HIỆU HÓA TÀI KHOẢN NGƯỜI DÙNG]
///
/// - Luồng đi (Request Flow):
///   HTTP PATCH /api/users/{id:int}/status
///     -> SetUserActiveEndpoint.MapSetUserActiveEndpoint(...)
///     -> Lấy UserId của Admin đang thao tác từ ClaimsPrincipal
///     -> IUserAdministrationService.SetActiveAsync(...) (điều phối xuống SetUserActiveHandler)
///     -> Trả về HTTP 200 OK hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class SetUserActiveEndpoint
{
    public static RouteHandlerBuilder MapSetUserActiveEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPatch("{id:int}/status", async (
            int id,
            [FromBody] UpdateUserActiveRequest request,
            ClaimsPrincipal user,
            IUserAdministrationService userAdministration,
            CancellationToken cancellationToken) =>
        {
            var result = await userAdministration.SetActiveAsync(
                id, request.IsActive, user.GetRequiredUserId(), cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.Ok(result.Data);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("UpdateUserStatus")
        .Produces<UserResponse>(StatusCodes.Status200OK);
    }
}

