using InternalMaintenance.Identity.Infrastructure.Features.Auth.ChangePassword;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.GetCurrentUser;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.GoogleLogin;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.Login;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.Logout;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.RefreshTokenSlice;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.SwitchRole;
using InternalMaintenance.Identity.Infrastructure.Features.Users.CreateUser;
using InternalMaintenance.Identity.Infrastructure.Features.Users.GetUserById;
using InternalMaintenance.Identity.Infrastructure.Features.Users.GetUsersPage;
using InternalMaintenance.Identity.Infrastructure.Features.Users.ResetUserPassword;
using InternalMaintenance.Identity.Infrastructure.Features.Users.SetUserActive;
using InternalMaintenance.Identity.Infrastructure.Features.Users.UpdateUser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Identity.Infrastructure.DependencyInjection;

/// <summary>
/// [MODULE ENDPOINT REGISTRATION - IDENTITY (AUTH &amp; USERS)]
/// Gom toàn bộ 13 Minimal API Endpoints của các Vertical Slices trong module Identity
/// vào 2 Route Groups `/api/auth` và `/api/users` theo chuẩn FullStackHero.
/// </summary>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("api/auth")
            .WithTags("Auth");

        authGroup.MapLoginEndpoint();
        authGroup.MapGoogleLoginEndpoint();
        authGroup.MapSwitchRoleEndpoint();
        authGroup.MapGetCurrentUserEndpoint();
        authGroup.MapChangePasswordEndpoint();
        authGroup.MapRefreshTokenEndpoint();
        authGroup.MapLogoutEndpoint();

        var usersGroup = app.MapGroup("api/users")
            .WithTags("Users");

        usersGroup.MapGetUsersPageEndpoint();
        usersGroup.MapGetUserByIdEndpoint();
        usersGroup.MapCreateUserEndpoint();
        usersGroup.MapUpdateUserEndpoint();
        usersGroup.MapSetUserActiveEndpoint();
        usersGroup.MapResetUserPasswordEndpoint();

        return app;
    }
}

