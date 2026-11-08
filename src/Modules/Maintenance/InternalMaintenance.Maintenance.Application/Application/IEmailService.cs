using InternalMaintenance.Maintenance.Domain;

namespace InternalMaintenance.Maintenance.Application;

public interface IEmailService
{
    Task SendNearBreachAlertAsync(
        MaintenanceTicket ticket,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null);
    Task SendBreachedAlertAsync(
        MaintenanceTicket ticket,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null);
    Task SendEscalationAlertAsync(
        MaintenanceTicket ticket,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null);
    Task SendSlaResultAsync(MaintenanceTicket ticket, bool isMet, CancellationToken cancellationToken = default);
}
