using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.ResetUserPassword;

/// <summary>
/// [MINIMAL API ENDPOINT - ĐẶT LẠI MẬT KHẨU TẠM THỜI CHO NGƯỜI DÙNG]
///
/// - Luồng đi (Request Flow):
///   HTTP PATCH /api/users/{id:int}/reset-password
///     -> ResetUserPasswordEndpoint.MapResetUserPasswordEndpoint(...)
///     -> Lấy UserId của Admin đang thao tác từ ClaimsPrincipal
///     -> IUserAdministrationService.ResetPasswordAsync(...) (điều phối xuống ResetUserPasswordHandler)
///     -> Trả về HTTP 200 OK hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class ResetUserPasswordEndpoint
{
    public static RouteHandlerBuilder MapResetUserPasswordEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPatch("{id:int}/reset-password", async (
            int id,
            [FromBody] ResetUserPasswordRequest request,
            ClaimsPrincipal user,
            IUserAdministrationService userAdministration,
            CancellationToken cancellationToken) =>
        {
            var result = await userAdministration.ResetPasswordAsync(
                id, request.TemporaryPassword, user.GetRequiredUserId(), cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.Ok(new { message = "User password has been reset successfully." });
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("ResetUserPassword");
    }
}

