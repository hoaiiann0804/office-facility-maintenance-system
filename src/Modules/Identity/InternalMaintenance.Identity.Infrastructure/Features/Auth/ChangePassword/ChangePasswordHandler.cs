using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.ChangePassword;

/// <summary>
/// [VERTICAL SLICE: CHANGE PASSWORD - NGƯỜI DÙNG TỰ ĐỔI MẬT KHẨU]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/auth/change-password (AuthController)
///           -> IAuthService.ChangePasswordAsync(userId, request)
///           -> ChangePasswordHandler.HandleAsync(userId, request)
///           -> Bước 1: Tìm User theo userId (401 nếu không tồn tại, 403 nếu bị khóa).
///           -> Bước 2: Kiểm tra độ dài mật khẩu mới qua IdentityPasswordPolicy.MeetsMinimumLength.
///           -> Bước 3: Xác minh mật khẩu hiện tại bằng BCrypt.Verify và đảm bảo mật khẩu mới khác mật khẩu cũ.
///           -> Bước 4: Cập nhật PasswordHash, tắt cờ MustChangePassword = false và lưu xuống DB.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Slice này chỉ cần duy nhất `AppDbContext`! Hoàn toàn không cần `JwtTokenService`, `IConfiguration`
///      hay `IOrganizationDirectory` như khi còn nằm chung trong `EfAuthService`.
/// </summary>
public sealed class ChangePasswordHandler(AppDbContext db)
{
    public async Task<ServiceResult> HandleAsync(
        int userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status401Unauthorized,
                "User not found");
        }

        if (!user.IsActive)
        {
            return ServiceResult.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator.");
        }

        if (!IdentityPasswordPolicy.MeetsMinimumLength(request.NewPassword))
        {
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                $"New password must be at least {IdentityPasswordPolicy.MinimumLength} characters");
        }

        var currentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!currentPasswordValid)
        {
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                "Current password is incorrect");
        }

        var isSamePassword = BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash);
        if (isSamePassword)
        {
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                "New password must be different from current password");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(StatusCodes.Status200OK);
    }
}

