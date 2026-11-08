using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Infrastructure.Features.Users.Common;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.GetUserById;

/// <summary>
/// [VERTICAL SLICE: GET USER BY ID - XEM CHI TIẾT TÀI KHOẢN NGƯỜI DÙNG]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/users/{id} (UsersController)
///           -> IUserAdministrationService.GetByIdAsync(id)
///           -> GetUserByIdHandler.HandleAsync(id)
///           -> Truy vấn User theo Id (AsNoTracking) + Điền tên phòng ban qua IOrganizationDirectory
///           -> Trả về UserResponse? (null nếu không tồn tại).
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Đóng vai trò là Slice đọc chi tiết độc lập, đồng thời được tái sử dụng bởi CreateUser, UpdateUser,
///      SetUserActive để nạp lại DTO phản hồi chuẩn xác sau khi ghi DB.
/// </summary>
public sealed class GetUserByIdHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory)
{
    public async Task<UserResponse?> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var response = await db.Users.AsNoTracking()
            .Where(user => user.Id == id)
            .Select(UserProjection.ToResponseProjection())
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null)
            await UserProjection.PopulateDepartmentNamesAsync(organizationDirectory, [response], cancellationToken);

        return response;
    }
}

