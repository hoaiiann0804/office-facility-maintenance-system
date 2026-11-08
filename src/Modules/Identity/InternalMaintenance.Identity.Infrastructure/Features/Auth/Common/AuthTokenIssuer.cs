using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.Common;

/// <summary>
/// [SHARED CONCERN TRONG NỘI BỘ MODULE IDENTITY - AUTH]
/// Đóng gói logic phát hành cặp AccessToken + RefreshToken và dựng AuthUserResponse.
///
/// Giải quyết vấn đề khi tái cấu trúc:
/// - Trước đây trong EfAuthService.cs (510 dòng), đoạn code gọi JwtTokenService sinh AccessToken,
///   tạo thực thể RefreshToken lưu vào AppDbContext và dựng LoginResponse bị lặp lại 3 lần ở:
///   LoginAsync, LoginWithGoogleAsync và SwitchRoleAsync.
/// - Tách vào AuthTokenIssuer giúp 3 Vertical Slices (Login, GoogleLogin, SwitchRole) dùng chung
///   một quy trình cấp phát token chuẩn xác mà vẫn giữ phạm vi đóng gói trong module Identity.
/// </summary>
public sealed class AuthTokenIssuer(
    AppDbContext db,
    JwtTokenService jwtTokenService,
    IOrganizationDirectory organizationDirectory)
{
    /// <summary>
    /// Sinh AccessToken, khởi tạo bản ghi RefreshToken trong DbContext, lưu thay đổi và trả về LoginResponse.
    /// </summary>
    public async Task<LoginResponse> IssueLoginResponseAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var expiresInMinutes = jwtTokenService.GetAccessTokenLifeTime();
        var refreshToken = jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtTokenService.GetRefreshTokenLifeTime())
        };

        db.RefreshTokens.Add(refreshTokenEntity);
        await db.SaveChangesAsync(cancellationToken);

        var department = await organizationDirectory.FindAsync(user.DepartmentId, cancellationToken);

        return new LoginResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresInMinutes = expiresInMinutes,
            RefreshToken = refreshToken,
            MustChangePassword = user.MustChangePassword,
            User = BuildAuthUserResponse(user, department)
        };
    }

    public async Task<AuthUserResponse> BuildAuthUserResponseAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var department = await organizationDirectory.FindAsync(user.DepartmentId, cancellationToken);
        return BuildAuthUserResponse(user, department);
    }

    public static AuthUserResponse BuildAuthUserResponse(
        User user,
        OrganizationDepartmentInfo? department) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        RoleName = user.Role!.Name,
        DepartmentId = user.DepartmentId,
        DepartmentName = department?.Name,
        DepartmentIsMaintenanceTeam = department?.IsMaintenanceTeam ?? false,
        IsActive = user.IsActive,
        MustChangePassword = user.MustChangePassword
    };
}

