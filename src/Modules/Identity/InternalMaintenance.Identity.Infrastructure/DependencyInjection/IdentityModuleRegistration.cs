using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.ChangePassword;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.Common;
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
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Identity.Infrastructure.DependencyInjection;

/// <summary>
/// [ĐĂNG KÝ DI CHO VERTICAL SLICES CỦA MODULE IDENTITY]
/// </summary>
public static class IdentityModuleRegistration
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<JwtTokenService>();
        services.AddScoped<AuthTokenIssuer>();

        // 7 Vertical Slice Handlers cho phân hệ Auth
        services.AddScoped<LoginHandler>();
        services.AddScoped<GoogleLoginHandler>();
        services.AddScoped<SwitchRoleHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<LogoutHandler>();

        // 6 Vertical Slice Handlers cho phân hệ User Administration
        services.AddScoped<GetUsersPageHandler>();
        services.AddScoped<GetUserByIdHandler>();
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<SetUserActiveHandler>();
        services.AddScoped<ResetUserPasswordHandler>();

        // Public Ports & Thin Façades
        services.AddScoped<ITechnicianDirectory, TechnicianDirectory>();
        services.AddScoped<IIdentityUserDirectory, IdentityUserDirectory>();
        services.AddScoped<IUserAdministrationService>(sp =>
            ActivatorUtilities.CreateInstance<UserAdministrationService>(sp));
        services.AddScoped<IAuthService>(sp =>
            ActivatorUtilities.CreateInstance<AuthService>(sp));
        return services;
    }
}
