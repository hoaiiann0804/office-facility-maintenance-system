using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>Cổng persistence của bình luận; entity EF và SaveChanges được giữ trong Maintenance.</summary>
public interface ITicketCommentStore
{
    Task<MaintenanceTicketAccessContext?> GetTicketContextAsync(
        int ticketId,
        CancellationToken cancellationToken);

    Task<int> AddAsync(
        int ticketId,
        int userId,
        string content,
        DateTime createdAt,
        CancellationToken cancellationToken);
}
