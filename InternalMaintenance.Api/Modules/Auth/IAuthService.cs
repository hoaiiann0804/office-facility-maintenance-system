using InternalMaintenance.Api.Common.Results;
using InternalMaintenance.Api.Modules.Auth.Contracts;

namespace InternalMaintenance.Api.Modules.Auth;

public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);

    Task<ServiceResult<AuthUserResponse>> GetCurrentUserAsync(int userId);

    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    Task<ServiceResult<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    Task<ServiceResult> LogoutAsync(LogoutRequest request);
}
