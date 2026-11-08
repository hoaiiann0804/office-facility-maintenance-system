using InternalMaintenance.Persistence;
using InternalMaintenance.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure;

/// <summary>
/// EF adapter của Identity: module gọi nhận projection, không nhận entity User hoặc quyền truy cập DbSet.
/// </summary>
public sealed class IdentityUserDirectory(AppDbContext db) : IIdentityUserDirectory
{
    public Task<string?> FindDisplayNameAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.FullName)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<string?> FindEmailAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Email)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int?> FindDepartmentIdAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<int>> FindUserIdsByDepartmentAsync(
        int? departmentId,
        CancellationToken cancellationToken = default)
    {
        // Caller chỉ cần ID để lọc ticket ở SQL; không tải User entity qua ranh giới module.
        return await db.Users.AsNoTracking()
            .Where(user => departmentId == null || user.DepartmentId == departmentId)
            .Select(user => user.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, string>> FindDisplayNamesAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<int, string>();

        return await db.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);
    }

    public Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default) =>
        db.Users.CountAsync(user => user.Role != null && user.Role.Name == roleName, cancellationToken);
}
