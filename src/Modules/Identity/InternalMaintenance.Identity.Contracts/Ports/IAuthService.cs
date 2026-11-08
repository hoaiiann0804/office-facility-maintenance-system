using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;

namespace InternalMaintenance.Identity.Application;

/// <summary>
/// Giao diện dịch vụ xác thực tài khoản và quản lý phiên người dùng.
/// [Lean Modular Monolith Refactoring]:
/// Di chuyển từ InternalMaintenance.Identity.Application sang InternalMaintenance.Identity.Contracts
/// để gom toàn bộ hợp đồng dịch vụ (Contracts) của Identity về một assembly duy nhất,
/// loại bỏ project Application rỗng chỉ có interface.
/// </summary>
public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);

    Task<ServiceResult<LoginResponse>> LoginWithGoogleAsync(GoogleLoginRequest request);

    Task<ServiceResult<LoginResponse>> SwitchRoleAsync(int userId, SwitchRoleRequest request);

    Task<ServiceResult<AuthUserResponse>> GetCurrentUserAsync(int userId);

    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    Task<ServiceResult<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    Task<ServiceResult> LogoutAsync(LogoutRequest request);
}
