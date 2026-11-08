using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Infrastructure.Features.Users.GetUserById;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.SetUserActive;

/// <summary>
/// [VERTICAL SLICE: SET USER ACTIVE - KHÓA / MỞ KHÓA TÀI KHOẢN &amp; THU HỒI TOKEN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> PATCH /api/users/{id}/status (UsersController)
///           -> IUserAdministrationService.SetActiveAsync(id, isActive, actorUserId)
///           -> SetUserActiveHandler.HandleAsync(id, isActive, actorUserId)
///           -> Bước 1: Kiểm tra User tồn tại, chặn Admin tự khóa tài khoản của chính mình (`user.Id == actorUserId && !isActive`).
///           -> Bước 2: Cập nhật cờ IsActive. Nếu là thao tác khóa (`!isActive`), tải toàn bộ RefreshTokens
///              chưa thu hồi của User đó và đánh dấu `IsRevoked = true` trong cùng 1 lần SaveChangesAsync.
///           -> Bước 3: Trả về UserResponse đã cập nhật.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Đảm bảo tính nguyên tử (Atomicity) giữa việc khóa tài khoản và thu hồi toàn bộ phiên đăng nhập
///      trong một lát cắt độc lập, dễ kiểm thử.
/// </summary>
public sealed class SetUserActiveHandler(
    AppDbContext db,
    GetUserByIdHandler getUserByIdHandler)
{
    public async Task<ServiceResult<UserResponse>> HandleAsync(
        int id,
        bool isActive,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
            return ServiceResult<UserResponse>.Fail(StatusCodes.Status404NotFound, "User not found");

        if (user.Id == actorUserId && !isActive)
            return ServiceResult<UserResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "You cannot deactivate your own account");

        if (user.IsActive == isActive)
            return ServiceResult<UserResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "User already has this status");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!isActive)
        {
            var activeTokens = await db.RefreshTokens
                .Where(token => token.UserId == id && !token.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var response = await getUserByIdHandler.HandleAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("Updated user could not be loaded.");

        return ServiceResult<UserResponse>.Success(response);
    }
}

