using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.Common;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.Login;

/// <summary>
/// [VERTICAL SLICE: LOGIN - ĐĂNG NHẬP BẰNG EMAIL &amp; MẬT KHẨU]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/auth/login (AuthController)
///           -> IAuthService.LoginAsync(request)
///           -> LoginHandler.HandleAsync(request)
///           -> Bước 1: Chuẩn hóa Email (Trim + ToLower) và tải User kèm Role từ DB.
///           -> Bước 2: Kiểm tra tài khoản tồn tại (401), đang hoạt động IsActive (403), và khớp BCrypt PasswordHash (401).
///           -> Bước 3: Cập nhật LastLoginAt = UtcNow, phát hành JWT AccessToken + RefreshToken qua AuthTokenIssuer.
///           -> Bước 4: Trả về ServiceResult&lt;LoginResponse&gt;.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách luồng đăng nhập truyền thống (Email/Password) ra khỏi Google SSO (vốn cần thư viện Google.Apis.Auth
///      và IConfiguration). Nhờ đó LoginHandler gọn nhẹ, không phụ thuộc vào cấu hình Google ClientId.
/// </summary>
public sealed class LoginHandler(
    AppDbContext db,
    AuthTokenIssuer tokenIssuer)
{
    public async Task<ServiceResult<LoginResponse>> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLower();

        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (user is null)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password");
        }

        if (!user.IsActive)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator.");
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password");
        }

        user.LastLoginAt = DateTime.UtcNow;

        var response = await tokenIssuer.IssueLoginResponseAsync(user, cancellationToken);
        return ServiceResult<LoginResponse>.Success(response);
    }
}

