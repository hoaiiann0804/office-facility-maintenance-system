using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>Cổng persistence: handler giữ quy tắc cập nhật, adapter chịu trách nhiệm EF Core.</summary>
public interface ITicketUpdateStore
{
    Task<MaintenanceTicket?> GetForUpdateAsync(int ticketId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken);
}
