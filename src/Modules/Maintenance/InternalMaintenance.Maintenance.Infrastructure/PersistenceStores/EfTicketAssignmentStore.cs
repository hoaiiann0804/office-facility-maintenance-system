using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>
/// [WRITE STORE SLICE: ASSIGN TICKET]
/// Adapter EF cho use-case phân công kỹ thuật viên chính/phụ và ghi lịch sử chuyển trạng thái.
/// </summary>
public sealed class EfTicketAssignmentStore(
    AppDbContext db,
    IIdentityUserDirectory identityUserDirectory,
    IEquipmentTicketContextReader equipmentReader,
    IVendorDirectory vendorDirectory) : ITicketAssignmentStore
{
    public Task<MaintenanceTicket?> GetForAssignmentAsync(int ticketId, CancellationToken cancellationToken) =>
        db.MaintenanceTickets
            .FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);

    public void AddStatusHistory(TicketStatusHistory history) => db.TicketStatusHistories.Add(history);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken) =>
        TicketMappingExtensions.LoadResponseByIdAsync(
            db,
            ticketId,
            identityUserDirectory,
            equipmentReader,
            vendorDirectory,
            cancellationToken);
}
