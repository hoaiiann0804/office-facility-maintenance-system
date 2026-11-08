using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.GetCurrentUser;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY THÔNG TIN USER HIỆN TẠI]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/auth/me
///     -> GetCurrentUserEndpoint.MapGetCurrentUserEndpoint(...)
///     -> Lấy UserId từ ClaimsPrincipal
///     -> IAuthService.GetCurrentUserAsync(...) (điều phối xuống GetCurrentUserHandler)
///     -> Trả về HTTP 200 OK kèm AuthUserResponse.
/// </summary>
public static class GetCurrentUserEndpoint
{
    public static RouteHandlerBuilder MapGetCurrentUserEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("me", async (
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            var currentUserId = user.GetRequiredUserId();
            var result = await authService.GetCurrentUserAsync(currentUserId);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .RequireAuthorization()
        .WithName("Me")
        .Produces<AuthUserResponse>(StatusCodes.Status200OK);
    }
}

