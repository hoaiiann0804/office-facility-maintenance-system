using InternalMaintenance.Persistence;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>
/// EF adapter của comment. Chỉ trả projection cần cho kiểm tra quyền và chỉ lưu entity trong
/// module Maintenance; API không cần truy cập DbSet TicketComments hoặc entity ticket.
/// </summary>
public sealed class EfTicketCommentStore(AppDbContext db) : ITicketCommentStore
{
    public Task<MaintenanceTicketAccessContext?> GetTicketContextAsync(
        int ticketId,
        CancellationToken cancellationToken) =>
        db.MaintenanceTickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId)
            .Select(ticket => new MaintenanceTicketAccessContext(
                ticket.Id,
                ticket.TicketCode,
                ticket.Status,
                ticket.EquipmentId,
                ticket.CreatedByUserId,
                ticket.AssignedTechnicianId,
                ticket.SupportTechnicianId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> AddAsync(
        int ticketId,
        int userId,
        string content,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var comment = new TicketComment
        {
            MaintenanceTicketId = ticketId,
            UserId = userId,
            Content = content,
            CreatedAt = createdAt
        };

        db.TicketComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        return comment.Id;
    }
}
