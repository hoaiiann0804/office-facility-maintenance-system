using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Infrastructure.Features.Users.Common;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.GetUsersPage;

/// <summary>
/// [VERTICAL SLICE: GET USERS PAGE - TRUY VẤN DANH SÁCH NGƯỜI DÙNG PHÂN TRANG]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/users (UsersController)
///           -> IUserAdministrationService.GetPageAsync(query)
///           -> GetUsersPageHandler.HandleAsync(query)
///           -> Bước 1: Lọc theo Keyword (FullName/Email), Role, DepartmentId, IsActive trên AppDbContext.Users (AsNoTracking).
///           -> Bước 2: Đếm tổng số bản ghi (CountAsync) + Phân trang (ApplyPaging) + Ánh xạ SQL (UserProjection).
///           -> Bước 3: Gọi IOrganizationDirectory.FindNamesAsync theo lô (Batch Lookup) để điền DepartmentName.
///           -> Bước 4: Trả về PagedResponse&lt;UserResponse&gt;.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách riêng luồng đọc danh sách người dùng có bộ lọc phức tạp và Batch Lookup tên phòng ban
///      khỏi các luồng ghi/cập nhật/khóa tài khoản.
/// </summary>
public sealed class GetUsersPageHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory)
{
    public async Task<PagedResponse<UserResponse>> HandleAsync(
        UserQuery query,
        CancellationToken cancellationToken = default)
    {
        var users = db.Users.AsNoTracking().AsQueryable();

        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
            users = users.Where(user => user.FullName.Contains(keyword) || user.Email.Contains(keyword));

        var role = query.Role?.Trim();
        if (!string.IsNullOrWhiteSpace(role))
            users = users.Where(user => user.Role!.Name == role);

        if (query.DepartmentId.HasValue)
            users = users.Where(user => user.DepartmentId == query.DepartmentId);

        if (query.IsActive.HasValue)
            users = users.Where(user => user.IsActive == query.IsActive);

        var total = await users.CountAsync(cancellationToken);
        var responses = await users
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .ApplyPaging(query)
            .Select(UserProjection.ToResponseProjection())
            .ToListAsync(cancellationToken);

        await UserProjection.PopulateDepartmentNamesAsync(organizationDirectory, responses, cancellationToken);
        return responses.ToPagedResponse(query, total);
    }
}

