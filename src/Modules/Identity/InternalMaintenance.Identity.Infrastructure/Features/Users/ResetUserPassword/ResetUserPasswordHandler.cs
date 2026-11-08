using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.ResetUserPassword;

/// <summary>
/// [VERTICAL SLICE: RESET USER PASSWORD - ADMIN CẤP LẠI MẬT KHẨU TẠM CHO NHÂN VIÊN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/users/{id}/reset-password (UsersController)
///           -> IUserAdministrationService.ResetPasswordAsync(id, temporaryPassword, actorUserId)
///           -> ResetUserPasswordHandler.HandleAsync(id, temporaryPassword, actorUserId)
///           -> Bước 1: Kiểm tra User tồn tại và chặn Admin tự reset mật khẩu của chính mình qua cổng quản trị.
///           -> Bước 2: Kiểm tra độ dài mật khẩu tạm qua IdentityPasswordPolicy.MeetsMinimumLength.
///           -> Bước 3: Băm mật khẩu tạm bằng BCrypt, bật cờ MustChangePassword = true và lưu xuống DB.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Slice này hoàn toàn độc lập, chỉ cần duy nhất `AppDbContext` mà không cần `IOrganizationDirectory`.
/// </summary>
public sealed class ResetUserPasswordHandler(AppDbContext db)
{
    public async Task<ServiceResult> HandleAsync(
        int id,
        string temporaryPassword,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
            return ServiceResult.Fail(StatusCodes.Status404NotFound, "User not found");

        if (user.Id == actorUserId)
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                "Please use Change Password to update your own password");

        if (!IdentityPasswordPolicy.MeetsMinimumLength(temporaryPassword))
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                $"Temporary password must be at least {IdentityPasswordPolicy.MinimumLength} characters");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}

