using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.GoogleLogin;

/// <summary>
/// [MINIMAL API ENDPOINT - ĐĂNG NHẬP GOOGLE SSO]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/auth/google
///     -> GoogleLoginEndpoint.MapGoogleLoginEndpoint(...)
///     -> IAuthService.LoginWithGoogleAsync(...) (điều phối xuống GoogleLoginHandler)
///     -> Trả về HTTP 200 OK kèm LoginResponse hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class GoogleLoginEndpoint
{
    public static RouteHandlerBuilder MapGoogleLoginEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("google", async (
            [FromBody] GoogleLoginRequest request,
            IAuthService authService) =>
        {
            var result = await authService.LoginWithGoogleAsync(request);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .AllowAnonymous()
        .WithName("LoginWithGoogle")
        .Produces<LoginResponse>(StatusCodes.Status200OK);
    }
}

