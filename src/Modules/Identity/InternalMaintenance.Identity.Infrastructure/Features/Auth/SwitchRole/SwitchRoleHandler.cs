using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Identity.Infrastructure.Features.Auth.Common;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Auth.SwitchRole;

/// <summary>
/// [VERTICAL SLICE: SWITCH ROLE - CHUYỂN ĐỔI VAI TRÒ NHANH &amp; CẤP LẠI TOKEN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> POST /api/auth/switch-role (AuthController)
///           -> IAuthService.SwitchRoleAsync(userId, request)
///           -> SwitchRoleHandler.HandleAsync(userId, request)
///           -> Bước 1: Kiểm tra RoleName hợp lệ (Admin, Manager, Technician, Staff).
///           -> Bước 2: Tải User và kiểm tra trạng thái IsActive.
///           -> Bước 3: Nếu chuyển sang Manager hoặc Technician mà phòng ban hiện tại không phải đội bảo trì
///              (IsMaintenanceTeam != true), tự động hỏi IOrganizationDirectory.FindMaintenanceTeamOrDefaultAsync()
///              để gán User vào phòng bảo trì.
///           -> Bước 4: Cập nhật RoleId và cấp cặp JWT AccessToken + RefreshToken mới qua AuthTokenIssuer.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách riêng nghiệp vụ chuyển đổi vai trò (có logic tự động điều chuyển phòng ban bảo trì liên module)
///      ra khỏi các luồng đăng nhập chuẩn.
/// </summary>
public sealed class SwitchRoleHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory,
    AuthTokenIssuer tokenIssuer)
{
    public async Task<ServiceResult<LoginResponse>> HandleAsync(
        int userId,
        SwitchRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var targetRoleName = request.RoleName?.Trim();
        var validRoles = new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.Technician, UserRoles.Staff };

        if (string.IsNullOrWhiteSpace(targetRoleName) || !validRoles.Contains(targetRoleName))
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status400BadRequest,
                $"Invalid role. Valid roles: {string.Join(", ", validRoles)}");
        }

        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status404NotFound,
                "User not found");
        }

        if (!user.IsActive)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator.");
        }

        var targetRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName, cancellationToken);
        if (targetRole is null)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Target role does not exist in database");
        }

        var currentDepartment = await organizationDirectory.FindAsync(user.DepartmentId, cancellationToken);
        if ((targetRoleName == UserRoles.Manager || targetRoleName == UserRoles.Technician) &&
            currentDepartment?.IsMaintenanceTeam != true)
        {
            var maintenanceDept = await organizationDirectory.FindMaintenanceTeamOrDefaultAsync(cancellationToken);
            if (maintenanceDept != null)
            {
                user.DepartmentId = maintenanceDept.Id;
            }
        }

        user.RoleId = targetRole.Id;
        user.Role = targetRole;
        user.UpdatedAt = DateTime.UtcNow;

        var response = await tokenIssuer.IssueLoginResponseAsync(user, cancellationToken);
        return ServiceResult<LoginResponse>.Success(response);
    }
}

