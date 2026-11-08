using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Identity.Infrastructure.Features.Users.Common;
using InternalMaintenance.Identity.Infrastructure.Features.Users.GetUserById;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.UpdateUser;

/// <summary>
/// [VERTICAL SLICE: UPDATE USER - CẬP NHẬT THÔNG TIN, VAI TRÒ &amp; PHÒNG BAN CỦA NHÂN VIÊN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> PUT /api/users/{id} (UsersController)
///           -> IUserAdministrationService.UpdateAsync(id, request)
///           -> UpdateUserHandler.HandleAsync(id, request)
///           -> Bước 1: Kiểm tra User tồn tại (404 nếu không thấy).
///           -> Bước 2: Kiểm tra RoleId hợp lệ và chặn nâng quyền nhân viên thường lên Admin.
///           -> Bước 3: Kiểm tra DepartmentId tồn tại qua IOrganizationDirectory.
///           -> Bước 4: Cập nhật FullName, RoleId, DepartmentId, UpdatedAt và lưu xuống DB.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách riêng quy tắc chống leo thang đặc quyền ("Promoting a user to Admin is not allowed.")
///      vào Slice cập nhật hồ sơ nhân sự.
/// </summary>
public sealed class UpdateUserHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory,
    GetUserByIdHandler getUserByIdHandler)
{
    public async Task<ServiceResult<UserResponse>> HandleAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
            return ServiceResult<UserResponse>.Fail(StatusCodes.Status404NotFound, "User not found");

        if (!await db.Roles.AnyAsync(role => role.Id == request.RoleId, cancellationToken))
            return ServiceResult<UserResponse>.Fail(StatusCodes.Status400BadRequest, "Role not found");

        var adminRoleId = await db.Roles.AsNoTracking()
            .Where(role => role.Name == UserRoles.Admin)
            .Select(role => (int?)role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRoleId.HasValue && request.RoleId == adminRoleId.Value && user.RoleId != adminRoleId.Value)
            return ServiceResult<UserResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Promoting a user to Admin is not allowed.");

        if (!await UserProjection.DepartmentExistsAsync(organizationDirectory, request.DepartmentId, cancellationToken))
            return ServiceResult<UserResponse>.Fail(StatusCodes.Status400BadRequest, "Department not found");

        user.FullName = request.FullName.Trim();
        user.RoleId = request.RoleId;
        user.DepartmentId = request.DepartmentId;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var response = await getUserByIdHandler.HandleAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("Updated user could not be loaded.");

        return ServiceResult<UserResponse>.Success(response);
    }
}

