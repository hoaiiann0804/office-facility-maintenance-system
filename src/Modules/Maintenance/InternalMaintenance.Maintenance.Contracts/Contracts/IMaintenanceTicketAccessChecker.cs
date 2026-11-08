namespace InternalMaintenance.Maintenance.Contracts;

/// <summary>
/// Contract để module khác hỏi quyền trên ticket mà không gọi TicketAccessPolicy
/// hay phụ thuộc vào code/controller thuộc module Tickets.
/// </summary>
public interface IMaintenanceTicketAccessChecker
{
    Task<bool> CanAccessAsync(
        MaintenanceTicketAccessContext ticket,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default);
}
