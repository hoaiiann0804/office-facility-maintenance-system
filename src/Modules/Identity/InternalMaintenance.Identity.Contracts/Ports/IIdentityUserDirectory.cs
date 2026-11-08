namespace InternalMaintenance.Identity.Contracts;

/// <summary>
/// Hợp đồng đọc thông tin Identity mà module khác cần. Caller không truy cập bảng Users trực tiếp.
/// </summary>
public interface IIdentityUserDirectory
{
    Task<string?> FindDisplayNameAsync(int userId, CancellationToken cancellationToken = default);
    Task<string?> FindEmailAsync(int userId, CancellationToken cancellationToken = default);
    Task<int?> FindDepartmentIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<int>> FindUserIdsByDepartmentAsync(
        int? departmentId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> FindDisplayNamesAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken = default);
    Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default);
}
