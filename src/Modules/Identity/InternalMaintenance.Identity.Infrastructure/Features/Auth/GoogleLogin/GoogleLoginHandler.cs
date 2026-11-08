using Google.Apis.Auth;
using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.Common;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.GoogleLogin;

/// <summary>
/// [VERTICAL SLICE: GOOGLE LOGIN - ĐĂNG NHẬP SSO QUA GOOGLE &amp; TỰ ĐỘNG CẤP PHÁT USER]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/auth/google (AuthController)
///           -> IAuthService.LoginWithGoogleAsync(request)
///           -> GoogleLoginHandler.HandleAsync(request)
///           -> Bước 1: Xác thực Google ID Token qua GoogleJsonWebSignature.ValidateAsync (kiểm tra Audience = Google:ClientId).
///           -> Bước 2: Mở Database Transaction. Tìm User theo Email trong bảng Users.
///           -> Bước 3:
///              + Nếu chưa có User: Tự động tạo mới User với Role = Staff và phòng ban mặc định lấy qua
///                IOrganizationDirectory.GetDefaultDepartmentIdAsync().
///              + Nếu đã có User: Kiểm tra IsActive (403 nếu bị khóa) và cập nhật LastLoginAt.
///           -> Bước 4: Phát hành JWT AccessToken + lưu RefreshToken và Commit Transaction.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Cô lập hoàn toàn phụ thuộc vào thư viện bên thứ 3 (`Google.Apis.Auth`) và `IConfiguration["Google:ClientId"]`
///      vào duy nhất Slice này. Các Slice xác thực khác không còn bị kéo theo dependency của Google SSO.
/// </summary>
public sealed class GoogleLoginHandler(
    AppDbContext db,
    IConfiguration configuration,
    IOrganizationDirectory organizationDirectory,
    AuthTokenIssuer tokenIssuer)
{
    public async Task<ServiceResult<LoginResponse>> HandleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Google ID token is required.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                IssuedAtClockTolerance = TimeSpan.FromMinutes(5)
            };

            var clientId = configuration["Google:ClientId"];
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                settings.Audience = new[] { clientId };
            }

            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                $"Invalid Google authentication token: {ex.Message}");
        }
        catch (Exception)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Google authentication verification failed.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Email claim not provided by Google account.");
        }

        var email = payload.Email.Trim().ToLower();

        // Giữ cả 2 lần ghi (tạo User mới để lấy Id + tạo RefreshToken) trong cùng 1 transaction
        // để tránh trường hợp tạo User xong nhưng lỗi khi lưu RefreshToken.
        await using var loginTransaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (user is null)
        {
            var staffRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.Staff, cancellationToken)
                ?? await db.Roles.FirstAsync(cancellationToken);

            // Giao tiếp liên module qua IOrganizationDirectory để lấy phòng ban mặc định
            var defaultDepartmentId = await organizationDirectory.GetDefaultDepartmentIdAsync(cancellationToken);

            user = new User
            {
                FullName = !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name : payload.Email,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                RoleId = staffRole.Id,
                Role = staffRole,
                DepartmentId = defaultDepartmentId,
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            if (!user.IsActive)
            {
                return ServiceResult<LoginResponse>.Fail(
                    StatusCodes.Status403Forbidden,
                    "Your account has been deactivated. Please contact the administrator.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(payload.Name))
            {
                user.FullName = payload.Name;
            }
        }

        var response = await tokenIssuer.IssueLoginResponseAsync(user, cancellationToken);
        await loginTransaction.CommitAsync(cancellationToken);

        return ServiceResult<LoginResponse>.Success(response);
    }
}

