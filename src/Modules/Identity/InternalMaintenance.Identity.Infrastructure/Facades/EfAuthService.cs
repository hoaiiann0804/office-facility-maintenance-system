using InternalMaintenance.BuildingBlocks.Results;
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
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Identity.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BỘ ĐIỀU PHỐI MỎNG CHO CÁC LUỒNG XÁC THỰC (AUTH) CỦA MODULE IDENTITY]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    AuthController -> IAuthService (AuthService)
///      ├── LoginAsync(...)            ---> LoginHandler.HandleAsync(...)
///      ├── LoginWithGoogleAsync(...)  ---> GoogleLoginHandler.HandleAsync(...)
///      ├── SwitchRoleAsync(...)       ---> SwitchRoleHandler.HandleAsync(...)
///      ├── GetCurrentUserAsync(...)   ---> GetCurrentUserHandler.HandleAsync(...)
///      ├── ChangePasswordAsync(...)   ---> ChangePasswordHandler.HandleAsync(...)
///      ├── RefreshTokenAsync(...)     ---> RefreshTokenHandler.HandleAsync(...)
///      └── LogoutAsync(...)           ---> LogoutHandler.HandleAsync(...)
///
/// 2. VẤN ĐỀ TRƯỚC KHI TÁI CẤU TRÚC:
///    - Trước đây file EfAuthService.cs dài 510 dòng, trộn lẫn 7 luồng xác thực khác nhau (từ BCrypt Login,
///      Google OAuth2 Signature Validation, Role Switching, Password Change cho tới Refresh Token Rotation).
///    - Đoạn code phát hành AccessToken + RefreshToken bị lặp lại 3 lần.
///
/// 3. CÁCH GIẢI QUYẾT (ZERO BREAKING CHANGES):
///    - Phân rã thành 7 Vertical Slice Handlers trong `Features/Auth/` + 1 `AuthTokenIssuer` dùng chung.
///    - Giữ `AuthService` làm Thin Façade với `[ActivatorUtilitiesConstructor]` cho DI Container và
///      constructor tương thích ngược cho các bài test hiện có.
/// </summary>
public class AuthService : IAuthService
{
    private readonly LoginHandler _loginHandler;
    private readonly GoogleLoginHandler _googleLoginHandler;
    private readonly SwitchRoleHandler _switchRoleHandler;
    private readonly GetCurrentUserHandler _getCurrentUserHandler;
    private readonly ChangePasswordHandler _changePasswordHandler;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private readonly LogoutHandler _logoutHandler;

    [ActivatorUtilitiesConstructor]
    public AuthService(
        LoginHandler loginHandler,
        GoogleLoginHandler googleLoginHandler,
        SwitchRoleHandler switchRoleHandler,
        GetCurrentUserHandler getCurrentUserHandler,
        ChangePasswordHandler changePasswordHandler,
        RefreshTokenHandler refreshTokenHandler,
        LogoutHandler logoutHandler)
    {
        _loginHandler = loginHandler;
        _googleLoginHandler = googleLoginHandler;
        _switchRoleHandler = switchRoleHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _changePasswordHandler = changePasswordHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutHandler = logoutHandler;
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ các bài Unit/Integration Test khởi tạo trực tiếp AuthService.
    /// </summary>
    public AuthService(
        AppDbContext context,
        JwtTokenService jwtTokenService,
        IConfiguration configuration,
        IOrganizationDirectory organizationDirectory)
        : this(
            new LoginHandler(context, new AuthTokenIssuer(context, jwtTokenService, organizationDirectory)),
            new GoogleLoginHandler(context, configuration, organizationDirectory, new AuthTokenIssuer(context, jwtTokenService, organizationDirectory)),
            new SwitchRoleHandler(context, organizationDirectory, new AuthTokenIssuer(context, jwtTokenService, organizationDirectory)),
            new GetCurrentUserHandler(context, new AuthTokenIssuer(context, jwtTokenService, organizationDirectory)),
            new ChangePasswordHandler(context),
            new RefreshTokenHandler(context, jwtTokenService),
            new LogoutHandler(context))
    {
    }

    public Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request) =>
        _loginHandler.HandleAsync(request);

    public Task<ServiceResult<LoginResponse>> LoginWithGoogleAsync(GoogleLoginRequest request) =>
        _googleLoginHandler.HandleAsync(request);

    public Task<ServiceResult<LoginResponse>> SwitchRoleAsync(int userId, SwitchRoleRequest request) =>
        _switchRoleHandler.HandleAsync(userId, request);

    public Task<ServiceResult<AuthUserResponse>> GetCurrentUserAsync(int userId) =>
        _getCurrentUserHandler.HandleAsync(userId);

    public Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request) =>
        _changePasswordHandler.HandleAsync(userId, request);

    public Task<ServiceResult<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request) =>
        _refreshTokenHandler.HandleAsync(request);

    public Task<ServiceResult> LogoutAsync(LogoutRequest request) =>
        _logoutHandler.HandleAsync(request);
}
