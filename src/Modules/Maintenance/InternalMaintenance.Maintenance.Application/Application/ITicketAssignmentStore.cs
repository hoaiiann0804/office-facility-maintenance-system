using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

public interface ITicketAssignmentStore
{
    Task<MaintenanceTicket?> GetForAssignmentAsync(int ticketId, CancellationToken cancellationToken);
    void AddStatusHistory(TicketStatusHistory history);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken);
}
