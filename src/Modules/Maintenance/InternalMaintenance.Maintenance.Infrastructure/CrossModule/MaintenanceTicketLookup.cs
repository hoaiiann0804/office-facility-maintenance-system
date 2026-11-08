using InternalMaintenance.Persistence;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>
/// Maintenance sở hữu các truy vấn ticket liên module và chỉ trả contract/projection nhỏ.
/// Module gọi không cần biết entity, navigation hay cấu trúc bảng nội bộ của Maintenance.
/// </summary>
public sealed class MaintenanceTicketLookup(AppDbContext db) : IMaintenanceTicketLookup
{
    public Task<bool> HasEquipmentOccupyingTicketAsync(
        int equipmentId,
        CancellationToken cancellationToken = default) =>
        db.MaintenanceTickets.AnyAsync(ticket =>
            ticket.EquipmentId == equipmentId &&
            TicketWorkflowRules.EquipmentOccupyingStatuses.Contains(ticket.Status), cancellationToken);

    public Task<bool> HasAnyTicketForEquipmentAsync(
        int equipmentId,
        CancellationToken cancellationToken = default) =>
        db.MaintenanceTickets.AnyAsync(ticket => ticket.EquipmentId == equipmentId, cancellationToken);

    public Task<MaintenanceTicketAccessContext?> GetAccessContextAsync(
        int ticketId,
        CancellationToken cancellationToken = default) =>
        db.MaintenanceTickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId)
            // Module gọi chỉ cần các ID để kiểm tra quyền và TicketCode/Status cho file;
            // projection này tránh trả entity và Include navigation Identity/Assets.
            .Select(ticket => new MaintenanceTicketAccessContext(
                ticket.Id,
                ticket.TicketCode,
                ticket.Status,
                ticket.EquipmentId,
                ticket.CreatedByUserId,
                ticket.AssignedTechnicianId,
                ticket.SupportTechnicianId))
            .FirstOrDefaultAsync(cancellationToken);
}
