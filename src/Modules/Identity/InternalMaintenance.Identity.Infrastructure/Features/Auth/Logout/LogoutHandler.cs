using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.Logout;

/// <summary>
/// [VERTICAL SLICE: LOGOUT - ĐĂNG XUẤT &amp; THU HỒI REFRESH TOKEN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/auth/logout (AuthController)
///           -> IAuthService.LogoutAsync(request)
///           -> LogoutHandler.HandleAsync(request)
///           -> Tìm bản ghi RefreshToken theo chuỗi token:
///              + Nếu không tồn tại -> 401 Unauthorized.
///              + Nếu đã thu hồi trước đó -> Trả về 204 NoContent (Idempotent).
///              + Đánh dấu IsRevoked = true, RevokedAt = UtcNow và lưu xuống DB -> 204 NoContent.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Cô lập luồng đăng xuất chỉ phụ thuộc duy nhất vào `AppDbContext`.
/// </summary>
public sealed class LogoutHandler(AppDbContext db)
{
    public async Task<ServiceResult> HandleAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var storedRefreshToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (storedRefreshToken is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token not found");
        }

        if (storedRefreshToken.IsRevoked)
        {
            return ServiceResult.Success(StatusCodes.Status204NoContent);
        }

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(StatusCodes.Status204NoContent);
    }
}

