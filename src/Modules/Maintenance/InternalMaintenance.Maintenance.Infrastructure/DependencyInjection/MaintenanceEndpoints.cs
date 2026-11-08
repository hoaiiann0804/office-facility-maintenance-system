using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Infrastructure.Features.SlaOutbox;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.AssignTicket;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.ChangeTicketStatus;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.CreateTicket;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.CreateTicketComment;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketById;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketComments;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketHistory;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketsPage;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.UpdateTicket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.DependencyInjection;

/// <summary>
/// [MODULE ENDPOINT REGISTRATION - MAINTENANCE]
/// Gom toàn bộ 9 Minimal API Endpoints của Tickets (`/api/tickets`) và 2 Minimal API Endpoints
/// của SLA Outbox (`/api/maintenance/sla-notifications`) theo chuẩn FullStackHero.
/// </summary>
public static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder app)
    {
        var ticketsGroup = app.MapGroup("api/tickets")
            .WithTags("MaintenanceTickets")
            .RequireAuthorization();

        ticketsGroup.MapGetTicketsPageEndpoint();
        ticketsGroup.MapGetTicketByIdEndpoint();
        ticketsGroup.MapCreateTicketEndpoint();
        ticketsGroup.MapUpdateTicketEndpoint();
        ticketsGroup.MapAssignTicketEndpoint();
        ticketsGroup.MapChangeTicketStatusEndpoint();
        ticketsGroup.MapCreateTicketCommentEndpoint();
        ticketsGroup.MapGetTicketCommentsEndpoint();
        ticketsGroup.MapGetTicketHistoryEndpoint();

        var slaGroup = app.MapGroup("api/maintenance/sla-notifications")
            .WithTags("SlaNotificationOperations")
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{UserRoles.Admin},{UserRoles.Manager}" });

        slaGroup.MapSlaNotificationOperationsEndpoints();

        return app;
    }
}

