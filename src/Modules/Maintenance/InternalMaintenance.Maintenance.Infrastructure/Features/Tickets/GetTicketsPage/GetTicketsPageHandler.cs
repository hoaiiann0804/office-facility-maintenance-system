using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.Common;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketsPage;

/// <summary>
/// [VERTICAL SLICE: GET TICKETS PAGE - TRUY VẤN DANH SÁCH PHIẾU BẢO TRÌ PHÂN TRANG CÓ PHÂN QUYỀN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/tickets (MaintenanceTicketsController)
///           -> IMaintenanceTicketReader.GetPageAsync(query, actor)
///           -> GetTicketsPageHandler.HandleAsync(query, actor)
///           -> Bước 1: Áp dụng bộ lọc tầm nhìn theo vai trò (`TicketVisibilityFilter.ApplyVisibilityAsync`).
///           -> Bước 2: Lọc theo `Status`, `Priority`, `EquipmentId` trên `IQueryable&lt;MaintenanceTicket&gt;`.
///           -> Bước 3: Đếm tổng số bản ghi (`CountAsync`) + Phân trang (`ApplyPaging`) + Ánh xạ SQL (`ResponseProjection`).
///           -> Bước 4: Gọi Batch Lookup (`PopulateAllExternalDetailsAsync`) sang 3 modules (`Identity`, `Equipment`, `Vendors`)
///              để điền tên người tạo/kỹ thuật viên, mã/tên thiết bị và tên đối tác sửa chữa.
/// </summary>
public sealed class GetTicketsPageHandler(
    AppDbContext db,
    TicketVisibilityFilter visibilityFilter,
    IIdentityUserDirectory identityDirectory,
    IEquipmentTicketContextReader equipmentReader,
    IVendorDirectory vendorDirectory)
{
    public async Task<PagedResponse<MaintenanceTicketResponse>> HandleAsync(
        TicketQuery query,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default)
    {
        var visibleTickets = await visibilityFilter.ApplyVisibilityAsync(
            db.MaintenanceTickets.AsNoTracking(),
            actor,
            cancellationToken);

        var status = query.Status?.Trim();
        if (!string.IsNullOrWhiteSpace(status))
            visibleTickets = visibleTickets.Where(ticket => ticket.Status == status);

        var priority = query.Priority?.Trim();
        if (!string.IsNullOrWhiteSpace(priority))
            visibleTickets = visibleTickets.Where(ticket => ticket.Priority == priority);

        if (query.EquipmentId.HasValue)
            visibleTickets = visibleTickets.Where(ticket => ticket.EquipmentId == query.EquipmentId.Value);

        var totalItems = await visibleTickets.CountAsync(cancellationToken);
        var responses = await visibleTickets
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ThenBy(ticket => ticket.Id)
            .ApplyPaging(query)
            .Select(TicketMappingExtensions.ResponseProjection)
            .ToListAsync(cancellationToken);

        await TicketMappingExtensions.PopulateAllExternalDetailsAsync(
            responses,
            identityDirectory,
            equipmentReader,
            vendorDirectory,
            cancellationToken);

        return responses.ToPagedResponse(query, totalItems);
    }
}

