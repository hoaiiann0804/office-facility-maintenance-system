using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.RefreshTokenSlice;

/// <summary>
/// [MINIMAL API ENDPOINT - LÀM MỚI ACCESS TOKEN (REFRESH TOKEN ROTATION)]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/auth/refresh-token
///     -> RefreshTokenEndpoint.MapRefreshTokenEndpoint(...)
///     -> IAuthService.RefreshTokenAsync(...) (điều phối xuống RefreshTokenHandler)
///     -> Trả về HTTP 200 OK kèm RefreshTokenResponse.
/// </summary>
public static class RefreshTokenEndpoint
{
    public static RouteHandlerBuilder MapRefreshTokenEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("refresh-token", async (
            [FromBody] RefreshTokenRequest request,
            IAuthService authService) =>
        {
            var result = await authService.RefreshTokenAsync(request);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .Produces<RefreshTokenResponse>(StatusCodes.Status200OK);
    }
}

