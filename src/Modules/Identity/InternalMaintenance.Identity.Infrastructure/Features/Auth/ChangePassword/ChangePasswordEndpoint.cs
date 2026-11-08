using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.ChangePassword;

/// <summary>
/// [MINIMAL API ENDPOINT - ĐỔI MẬT KHẨU CÁ NHÂN]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/auth/change-password
///     -> ChangePasswordEndpoint.MapChangePasswordEndpoint(...)
///     -> Lấy UserId từ ClaimsPrincipal
///     -> IAuthService.ChangePasswordAsync(...) (điều phối xuống ChangePasswordHandler)
///     -> Trả về HTTP 200 OK hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class ChangePasswordEndpoint
{
    public static RouteHandlerBuilder MapChangePasswordEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("change-password", async (
            [FromBody] ChangePasswordRequest request,
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            var currentUserId = user.GetRequiredUserId();
            var result = await authService.ChangePasswordAsync(currentUserId, request);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.Ok(new
            {
                message = "Password changed successfully. PLease login again. "
            });
        })
        .RequireAuthorization()
        .WithName("ChangePassword");
    }
}

