using InternalMaintenance.Persistence;
using InternalMaintenance.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure;

/// <summary>Identity trả projection technician để Maintenance không phụ thuộc entity User.</summary>
public sealed class TechnicianDirectory(AppDbContext db) : ITechnicianDirectory
{
    public Task<TechnicianRecord?> FindAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new TechnicianRecord(
                user.Id,
                user.Role == null ? null : user.Role.Name,
                user.DepartmentId,
                user.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
}
