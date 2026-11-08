using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.Login;

/// <summary>
/// [MINIMAL API ENDPOINT - ĐĂNG NHẬP EMAIL/PASSWORD]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/auth/login
///     -> LoginEndpoint.MapLoginEndpoint(...)
///     -> IAuthService.LoginAsync(...) (điều phối xuống LoginHandler)
///     -> Trả về HTTP 200 OK kèm LoginResponse hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class LoginEndpoint
{
    public static RouteHandlerBuilder MapLoginEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("login", async (
            [FromBody] LoginRequest request,
            IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .AllowAnonymous()
        .WithName("Login")
        .Produces<LoginResponse>(StatusCodes.Status200OK);
    }
}

