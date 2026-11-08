using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.RefreshTokenSlice;

/// <summary>
/// [VERTICAL SLICE: REFRESH TOKEN - LÀM MỚI ACCESS TOKEN &amp; XOAY VÒNG REFRESH TOKEN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/auth/refresh (AuthController)
///           -> IAuthService.RefreshTokenAsync(request)
///           -> RefreshTokenHandler.HandleAsync(request)
///           -> Bước 1: Tìm bản ghi RefreshToken kèm User và Role trong DB.
///           -> Bước 2: Kiểm tra token tồn tại, chưa bị thu hồi (IsRevoked == false), chưa hết hạn (ExpiresAt &gt; UtcNow)
///              và tài khoản User vẫn đang hoạt động (IsActive == true).
///           -> Bước 3: Thu hồi RefreshToken cũ (IsRevoked = true, RevokedAt = UtcNow), sinh AccessToken mới
///              và RefreshToken mới (Token Rotation) rồi lưu trong cùng 1 lần SaveChangesAsync.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách riêng cơ chế xoay vòng Refresh Token (Token Rotation). Slice này chỉ phụ thuộc `AppDbContext`
///      và `JwtTokenService`, không cần truy vấn sang module `Organization`.
/// </summary>
public sealed class RefreshTokenHandler(
    AppDbContext db,
    JwtTokenService jwtTokenService)
{
    public async Task<ServiceResult<RefreshTokenResponse>> HandleAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var storedRefreshToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (storedRefreshToken is null)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token not found");
        }

        if (storedRefreshToken.IsRevoked)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token revoked");
        }

        if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token expired");
        }

        var user = storedRefreshToken.User;
        if (!user.IsActive)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated.");
        }

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtTokenService.GetRefreshTokenLifeTime())
        };

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        db.RefreshTokens.Add(refreshTokenEntity);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<RefreshTokenResponse>.Success(
            new RefreshTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                TokenType = "Bearer",
                ExpiresInMinutes = jwtTokenService.GetAccessTokenLifeTime()
            });
    }
}

