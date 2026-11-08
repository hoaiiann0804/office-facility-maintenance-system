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
/// [WRITE STORE SLICE: CHANGE TICKET STATUS]
/// Adapter EF cho use-case chuyển trạng thái phiếu bảo trì (bao gồm ghi nhật ký gửi/nhận Vendor
/// và lịch sử đổi trạng thái trong cùng một Unit of Work).
/// </summary>
public sealed class EfTicketStatusChangeStore(
    AppDbContext db,
    IIdentityUserDirectory identityUserDirectory,
    IEquipmentTicketContextReader equipmentReader,
    IVendorDirectory vendorDirectory) : ITicketStatusChangeStore
{
    public Task<MaintenanceTicket?> GetForStatusChangeAsync(int ticketId, CancellationToken cancellationToken) =>
        db.MaintenanceTickets
            .FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);

    public Task<TicketVendorLog?> GetOpenVendorLogAsync(int ticketId, CancellationToken cancellationToken) =>
        db.TicketVendorLogs
            .Where(log => log.MaintenanceTicketId == ticketId && log.ActualReturnDate == null)
            .OrderByDescending(log => log.DispatchedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void AddVendorLog(TicketVendorLog vendorLog) => db.TicketVendorLogs.Add(vendorLog);

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
