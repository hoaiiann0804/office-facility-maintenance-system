using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Infrastructure.Features.Users.CreateUser;
using InternalMaintenance.Identity.Infrastructure.Features.Users.GetUserById;
using InternalMaintenance.Identity.Infrastructure.Features.Users.GetUsersPage;
using InternalMaintenance.Identity.Infrastructure.Features.Users.ResetUserPassword;
using InternalMaintenance.Identity.Infrastructure.Features.Users.SetUserActive;
using InternalMaintenance.Identity.Infrastructure.Features.Users.UpdateUser;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Identity.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BỘ ĐIỀU PHỐI MỎNG CHO QUẢN TRỊ NGƯỜI DÙNG (USERS) CỦA MODULE IDENTITY]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    UsersController -> IUserAdministrationService (UserAdministrationService)
///      ├── GetPageAsync(...)        ---> GetUsersPageHandler.HandleAsync(...)
///      ├── GetByIdAsync(...)        ---> GetUserByIdHandler.HandleAsync(...)
///      ├── CreateAsync(...)         ---> CreateUserHandler.HandleAsync(...)
///      ├── UpdateAsync(...)         ---> UpdateUserHandler.HandleAsync(...)
///      ├── SetActiveAsync(...)      ---> SetUserActiveHandler.HandleAsync(...)
///      └── ResetPasswordAsync(...)  ---> ResetUserPasswordHandler.HandleAsync(...)
///
/// 2. VẤN ĐỀ TRƯỚC KHI TÁI CẤU TRÚC:
///    - Trước đây EfUserAdministrationService.cs ôm toàn bộ 6 nghiệp vụ quản trị nhân sự (phân trang,
///      tạo user, chặn leo thang quyền Admin, khóa tài khoản thu hồi token, reset mật khẩu tạm).
///
/// 3. CÁCH GIẢI QUYẾT (ZERO BREAKING CHANGES):
///    - Tách thành 6 Vertical Slice Handlers độc lập trong `Features/Users/`.
///    - Giữ `UserAdministrationService` làm Thin Façade với `[ActivatorUtilitiesConstructor]` cho DI
///      và constructor tương thích ngược cho các bài test hiện hữu.
/// </summary>
public sealed class UserAdministrationService : IUserAdministrationService
{
    private readonly GetUsersPageHandler _getUsersPageHandler;
    private readonly GetUserByIdHandler _getUserByIdHandler;
    private readonly CreateUserHandler _createUserHandler;
    private readonly UpdateUserHandler _updateUserHandler;
    private readonly SetUserActiveHandler _setUserActiveHandler;
    private readonly ResetUserPasswordHandler _resetUserPasswordHandler;

    [ActivatorUtilitiesConstructor]
    public UserAdministrationService(
        GetUsersPageHandler getUsersPageHandler,
        GetUserByIdHandler getUserByIdHandler,
        CreateUserHandler createUserHandler,
        UpdateUserHandler updateUserHandler,
        SetUserActiveHandler setUserActiveHandler,
        ResetUserPasswordHandler resetUserPasswordHandler)
    {
        _getUsersPageHandler = getUsersPageHandler;
        _getUserByIdHandler = getUserByIdHandler;
        _createUserHandler = createUserHandler;
        _updateUserHandler = updateUserHandler;
        _setUserActiveHandler = setUserActiveHandler;
        _resetUserPasswordHandler = resetUserPasswordHandler;
    }

    /// <summary>
    /// Constructor tương thích ngược phục vụ các bài Unit/Integration Test khởi tạo trực tiếp UserAdministrationService.
    /// </summary>
    public UserAdministrationService(
        AppDbContext db,
        IOrganizationDirectory organizationDirectory)
        : this(
            new GetUsersPageHandler(db, organizationDirectory),
            new GetUserByIdHandler(db, organizationDirectory),
            new CreateUserHandler(db, organizationDirectory, new GetUserByIdHandler(db, organizationDirectory)),
            new UpdateUserHandler(db, organizationDirectory, new GetUserByIdHandler(db, organizationDirectory)),
            new SetUserActiveHandler(db, new GetUserByIdHandler(db, organizationDirectory)),
            new ResetUserPasswordHandler(db))
    {
    }

    public Task<PagedResponse<UserResponse>> GetPageAsync(
        UserQuery query,
        CancellationToken cancellationToken = default) =>
        _getUsersPageHandler.HandleAsync(query, cancellationToken);

    public Task<UserResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _getUserByIdHandler.HandleAsync(id, cancellationToken);

    public Task<ServiceResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default) =>
        _createUserHandler.HandleAsync(request, cancellationToken);

    public Task<ServiceResult<UserResponse>> UpdateAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default) =>
        _updateUserHandler.HandleAsync(id, request, cancellationToken);

    public Task<ServiceResult<UserResponse>> SetActiveAsync(
        int id,
        bool isActive,
        int actorUserId,
        CancellationToken cancellationToken = default) =>
        _setUserActiveHandler.HandleAsync(id, isActive, actorUserId, cancellationToken);

    public Task<ServiceResult> ResetPasswordAsync(
        int id,
        string temporaryPassword,
        int actorUserId,
        CancellationToken cancellationToken = default) =>
        _resetUserPasswordHandler.HandleAsync(id, temporaryPassword, actorUserId, cancellationToken);
}
