using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.Logout;

/// <summary>
/// [MINIMAL API ENDPOINT - ĐĂNG XUẤT VÀ THU HỒI REFRESH TOKEN]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/auth/logout
///     -> LogoutEndpoint.MapLogoutEndpoint(...)
///     -> IAuthService.LogoutAsync(...) (điều phối xuống LogoutHandler)
///     -> Trả về HTTP 204 NoContent hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class LogoutEndpoint
{
    public static RouteHandlerBuilder MapLogoutEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("logout", async (
            [FromBody] LogoutRequest request,
            IAuthService authService) =>
        {
            var result = await authService.LogoutAsync(request);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .AllowAnonymous()
        .WithName("Logout")
        .Produces(StatusCodes.Status204NoContent);
    }
}

