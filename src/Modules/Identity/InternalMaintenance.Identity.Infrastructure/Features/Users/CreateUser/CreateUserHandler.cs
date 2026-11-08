using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Identity.Infrastructure.Features.Users.Common;
using InternalMaintenance.Identity.Infrastructure.Features.Users.GetUserById;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.CreateUser;

/// <summary>
/// [VERTICAL SLICE: CREATE USER - ADMIN TẠO TÀI KHOẢN NHÂN VIÊN MỚI]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/users (UsersController)
///           -> IUserAdministrationService.CreateAsync(request)
///           -> CreateUserHandler.HandleAsync(request)
///           -> Bước 1: Kiểm tra trùng Email trong bảng Users.
///           -> Bước 2: Kiểm tra RoleId tồn tại và chặn tạo tài khoản có quyền Admin ("Creating a new Admin account is not allowed.").
///           -> Bước 3: Kiểm tra DepartmentId tồn tại qua IOrganizationDirectory.
///           -> Bước 4: Băm mật khẩu tạm (TemporaryPassword) bằng BCrypt, bật cờ MustChangePassword = true và lưu DB.
///           -> Bước 5: Trả về ServiceResult&lt;UserResponse&gt; (201 Created).
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Cô lập toàn bộ quy tắc bảo mật khi tạo nhân sự mới (chặn tạo thêm Admin, bắt buộc đổi mật khẩu lần đầu)
///      vào một Slice duy nhất.
/// </summary>
public sealed class CreateUserHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory,
    GetUserByIdHandler getUserByIdHandler)
{
    public async Task<ServiceResult<UserResponse>> HandleAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLower();

        if (await db.Users.AnyAsync(user => user.Email.ToLower() == email, cancellationToken))
            return ServiceResult<UserResponse>.Fail(StatusCodes.Status400BadRequest, "User created exists ");

        if (!await db.Roles.AnyAsync(role => role.Id == request.RoleId, cancellationToken))
            return ServiceResult<UserResponse>.Fail(StatusCodes.Status400BadRequest, "Role not found");

        var adminRoleId = await db.Roles.AsNoTracking()
            .Where(role => role.Name == UserRoles.Admin)
            .Select(role => (int?)role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRoleId.HasValue && request.RoleId == adminRoleId.Value)
            return ServiceResult<UserResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Creating a new Admin account is not allowed.");

        if (!await UserProjection.DepartmentExistsAsync(organizationDirectory, request.DepartmentId, cancellationToken))
            return ServiceResult<UserResponse>.Fail(StatusCodes.Status400BadRequest, "Department not found");

        var user = new User
        {
            FullName = fullName,
            Email = email,
            RoleId = request.RoleId,
            DepartmentId = request.DepartmentId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.TemporaryPassword),
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var response = await getUserByIdHandler.HandleAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("Created user could not be loaded.");

        return ServiceResult<UserResponse>.Success(response, StatusCodes.Status201Created);
    }
}

