using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.SwitchRole;

/// <summary>
/// [MINIMAL API ENDPOINT - CHUYỂN ĐỔI VAI TRÒ (DEV/DEMO)]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/auth/switch-role
///     -> SwitchRoleEndpoint.MapSwitchRoleEndpoint(...)
///     -> Lấy UserId từ ClaimsPrincipal
///     -> IAuthService.SwitchRoleAsync(...) (điều phối xuống SwitchRoleHandler)
///     -> Trả về HTTP 200 OK kèm LoginResponse mới.
/// </summary>
public static class SwitchRoleEndpoint
{
    public static RouteHandlerBuilder MapSwitchRoleEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("switch-role", async (
            [FromBody] SwitchRoleRequest request,
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            var currentUserId = user.GetRequiredUserId();
            var result = await authService.SwitchRoleAsync(currentUserId, request);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .RequireAuthorization()
        .WithName("SwitchRole")
        .Produces<LoginResponse>(StatusCodes.Status200OK);
    }
}

