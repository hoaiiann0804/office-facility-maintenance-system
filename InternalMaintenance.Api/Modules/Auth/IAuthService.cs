using InternalMaintenance.Api.Modules.Auth.Contracts;

namespace InternalMaintenance.Api.Modules.Auth;

public interface IAuthService
{
    Task<AuthServiceResult<LoginResponse>> LoginAsync(LoginRequest request);

    Task<AuthServiceResult<AuthUserResponse>> GetCurrentUserAsync(int userId);

    Task<AuthServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    Task<AuthServiceResult<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    Task<AuthServiceResult> LogoutAsync(LogoutRequest request);
}
