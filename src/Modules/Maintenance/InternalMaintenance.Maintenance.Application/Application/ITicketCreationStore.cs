using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

public interface ITicketCreationStore
{
    Task<bool> HasEquipmentOccupyingTicketAsync(int equipmentId, CancellationToken cancellationToken);
    /// <summary>Returns false only when the database's filtered unique index rejected a duplicate ticket.</summary>
    Task<bool> TryAddAndSaveAsync(MaintenanceTicket ticket, CancellationToken cancellationToken);
    Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken);
}
