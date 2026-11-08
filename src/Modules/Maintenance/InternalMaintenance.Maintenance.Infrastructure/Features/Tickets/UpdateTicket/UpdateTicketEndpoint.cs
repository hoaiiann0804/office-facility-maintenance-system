using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.UpdateTicket;

public sealed class UpdateMaintenanceTicketEndpointRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}

/// <summary>
/// [MINIMAL API ENDPOINT - CẬP NHẬT THÔNG TIN PHIẾU BẢO TRÌ]
///
/// - Luồng đi (Request Flow):
///   HTTP PUT /api/tickets/{id:int}
///     -> UpdateTicketEndpoint.MapUpdateTicketEndpoint(...)
///     -> UpdateMaintenanceTicketHandler.Handle(...)
///     -> Trả về HTTP 200 OK hoặc 404/403/400.
/// </summary>
public static class UpdateTicketEndpoint
{
    public static RouteHandlerBuilder MapUpdateTicketEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPut("{id:int}", async (
            int id,
            [FromBody] UpdateMaintenanceTicketEndpointRequest request,
            ClaimsPrincipal user,
            UpdateMaintenanceTicketHandler handler,
            CancellationToken cancellationToken) =>
        {
            var outcome = await handler.Handle(
                new UpdateMaintenanceTicketCommand(
                    id,
                    request.Title,
                    request.Description,
                    request.Priority,
                    user.GetRequiredUserId(),
                    user.GetRole(),
                    user.GetDepartmentId()),
                cancellationToken);

            if (outcome.IsSuccess)
                return Results.Ok(outcome.Response);

            return outcome.Error switch
            {
                TicketUpdateError.NotFound => Results.NotFound(new { message = outcome.Message }),
                TicketUpdateError.Forbidden => Results.Forbid(),
                _ => Results.BadRequest(new { message = outcome.Message })
            };
        })
        .RequireAuthorization()
        .WithName("UpdateTicket")
        .Produces<MaintenanceTicketResponse>(StatusCodes.Status200OK);
    }
}

