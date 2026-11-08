using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.Common;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketById;

/// <summary>
/// [VERTICAL SLICE: GET TICKET BY ID - XEM CHI TIẾT PHIẾU BẢO TRÌ CÓ KIỂM TRA QUYỀN TẦM NHÌN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/tickets/{id} (MaintenanceTicketsController)
///           -> IMaintenanceTicketReader.GetByIdAsync(ticketId, actor)
///           -> GetTicketByIdHandler.HandleAsync(ticketId, actor)
///           -> Bước 1: Áp dụng bộ lọc quyền tầm nhìn (`TicketVisibilityFilter.ApplyVisibilityAsync`).
///           -> Bước 2: Truy vấn `MaintenanceTicket` theo `ticketId` (`AsNoTracking`) và chiếu sang `MaintenanceTicketResponse`.
///           -> Bước 3: Điền thông tin liên module (`Identity`, `Equipment`, `Vendors`) và trả về kết quả.
/// </summary>
public sealed class GetTicketByIdHandler(
    AppDbContext db,
    TicketVisibilityFilter visibilityFilter,
    IIdentityUserDirectory identityDirectory,
    IEquipmentTicketContextReader equipmentReader,
    IVendorDirectory vendorDirectory)
{
    public async Task<MaintenanceTicketResponse?> HandleAsync(
        int ticketId,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default)
    {
        var visibleTickets = await visibilityFilter.ApplyVisibilityAsync(
            db.MaintenanceTickets.AsNoTracking(),
            actor,
            cancellationToken);

        var response = await visibleTickets
            .Where(ticket => ticket.Id == ticketId)
            .Select(TicketMappingExtensions.ResponseProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null)
        {
            await TicketMappingExtensions.PopulateAllExternalDetailsAsync(
                [response],
                identityDirectory,
                equipmentReader,
                vendorDirectory,
                cancellationToken);
        }

        return response;
    }
}

