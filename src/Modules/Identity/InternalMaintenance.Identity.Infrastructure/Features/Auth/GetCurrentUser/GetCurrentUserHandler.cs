using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.Common;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.GetCurrentUser;

/// <summary>
/// [VERTICAL SLICE: GET CURRENT USER - LẤY HỒ SƠ NGƯỜI DÙNG ĐANG ĐĂNG NHẬP]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/auth/me (AuthController)
///           -> IAuthService.GetCurrentUserAsync(userId)
///           -> GetCurrentUserHandler.HandleAsync(userId)
///           -> Truy vấn User kèm Role từ AppDbContext, kiểm tra IsActive,
///              lấy thêm thông tin Department qua AuthTokenIssuer (IOrganizationDirectory)
///           -> Trả về ServiceResult&lt;AuthUserResponse&gt;.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách riêng truy vấn đọc thông tin hồ sơ cá nhân (`/api/auth/me`) ra khỏi các luồng ghi/cấp phát token.
/// </summary>
public sealed class GetCurrentUserHandler(
    AppDbContext db,
    AuthTokenIssuer tokenIssuer)
{
    public async Task<ServiceResult<AuthUserResponse>> HandleAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult<AuthUserResponse>.Fail(
                StatusCodes.Status404NotFound,
                "User not found");
        }

        if (!user.IsActive)
        {
            return ServiceResult<AuthUserResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator.");
        }

        var response = await tokenIssuer.BuildAuthUserResponseAsync(user, cancellationToken);
        return ServiceResult<AuthUserResponse>.Success(response);
    }
}

