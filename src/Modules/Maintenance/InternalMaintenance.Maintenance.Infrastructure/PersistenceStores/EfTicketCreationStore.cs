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
/// [WRITE STORE SLICE: CREATE TICKET]
/// Adapter EF lưu phiếu bảo trì mới, xử lý xung đột Unique Index khi 2 request cùng tạo ticket
/// cho một thiết bị đồng thời, và nạp lại DTO phản hồi qua `TicketMappingExtensions.LoadResponseByIdAsync`.
/// </summary>
public sealed class EfTicketCreationStore(
    AppDbContext db,
    IIdentityUserDirectory identityUserDirectory,
    IEquipmentTicketContextReader equipmentReader,
    IVendorDirectory vendorDirectory) : ITicketCreationStore
{
    public Task<bool> HasEquipmentOccupyingTicketAsync(int equipmentId, CancellationToken cancellationToken) =>
        db.MaintenanceTickets.AnyAsync(ticket =>
            ticket.EquipmentId == equipmentId &&
            TicketWorkflowRules.EquipmentOccupyingStatuses.Contains(ticket.Status), cancellationToken);

    public async Task<bool> TryAddAndSaveAsync(MaintenanceTicket ticket, CancellationToken cancellationToken)
    {
        db.MaintenanceTickets.Add(ticket);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            if (await HasEquipmentOccupyingTicketAsync(ticket.EquipmentId, cancellationToken))
                return false;

            throw;
        }
    }

    public Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken) =>
        TicketMappingExtensions.LoadResponseByIdAsync(
            db,
            ticketId,
            identityUserDirectory,
            equipmentReader,
            vendorDirectory,
            cancellationToken);
}
