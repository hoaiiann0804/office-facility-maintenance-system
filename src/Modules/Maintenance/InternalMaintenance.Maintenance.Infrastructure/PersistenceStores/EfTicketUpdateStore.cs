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
/// [WRITE STORE SLICE: UPDATE TICKET]
/// Adapter EF cho use-case cập nhật tiêu đề, mô tả và mức độ ưu tiên (tự động tính lại hạn SLA).
/// </summary>
public sealed class EfTicketUpdateStore(
    AppDbContext db,
    IIdentityUserDirectory identityDirectory,
    IEquipmentTicketContextReader equipmentReader,
    IVendorDirectory vendorDirectory) : ITicketUpdateStore
{
    public Task<MaintenanceTicket?> GetForUpdateAsync(int ticketId, CancellationToken cancellationToken) =>
        db.MaintenanceTickets.FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task<MaintenanceTicketResponse?> GetResponseAsync(
        int ticketId,
        CancellationToken cancellationToken) =>
        TicketMappingExtensions.LoadResponseByIdAsync(
            db,
            ticketId,
            identityDirectory,
            equipmentReader,
            vendorDirectory,
            cancellationToken);
}
