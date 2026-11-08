using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.BuildingBlocks.Results;
using InternalMaintenance.Identity.Contracts;

namespace InternalMaintenance.Identity.Application;

/// <summary>
/// Contract use case quản trị người dùng. UsersController không nhận AppDbContext và không
/// thao tác trực tiếp với bảng Identity hay bảng của Organization.
/// [Lean Modular Monolith Refactoring]:
/// Di chuyển từ InternalMaintenance.Identity.Application sang InternalMaintenance.Identity.Contracts.
/// </summary>
public interface IUserAdministrationService
{
    Task<PagedResponse<UserResponse>> GetPageAsync(UserQuery query, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserResponse>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserResponse>> SetActiveAsync(int id, bool isActive, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> ResetPasswordAsync(int id, string temporaryPassword, int actorUserId, CancellationToken cancellationToken = default);
}
