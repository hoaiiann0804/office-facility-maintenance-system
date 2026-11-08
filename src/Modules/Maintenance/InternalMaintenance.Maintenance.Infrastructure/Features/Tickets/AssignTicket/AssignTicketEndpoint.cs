using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.AssignTicket;

public sealed class AssignMaintenanceTicketEndpointRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "AssignedTechnicianId must be greater than 0")]
    public int AssignedTechnicianId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SupportTechnicianId must be greater than 0")]
    public int? SupportTechnicianId { get; set; }

    [StringLength(500, ErrorMessage = "Note must not exceed 500 characters")]
    public string? Note { get; set; }
}

/// <summary>
/// [MINIMAL API ENDPOINT - PHÂN CÔNG KỸ THUẬT VIÊN XỬ LÝ TICKET]
///
/// - Luồng đi (Request Flow):
///   HTTP PATCH /api/tickets/{id:int}/assign
///     -> AssignTicketEndpoint.MapAssignTicketEndpoint(...)
///     -> AssignMaintenanceTicketHandler.Handle(...)
///     -> Trả về HTTP 200 OK hoặc 404/403/400.
/// </summary>
public static class AssignTicketEndpoint
{
    public static RouteHandlerBuilder MapAssignTicketEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPatch("{id:int}/assign", async (
            int id,
            [FromBody] AssignMaintenanceTicketEndpointRequest request,
            ClaimsPrincipal user,
            AssignMaintenanceTicketHandler handler,
            CancellationToken cancellationToken) =>
        {
            var outcome = await handler.Handle(
                new AssignMaintenanceTicketCommand(
                    id,
                    request.AssignedTechnicianId,
                    request.SupportTechnicianId,
                    request.Note,
                    user.GetRequiredUserId(),
                    user.GetRole(),
                    user.GetDepartmentId()),
                cancellationToken);

            if (outcome.IsSuccess)
                return Results.Ok(outcome.Response);

            return outcome.Error switch
            {
                TicketAssignmentError.NotFound => Results.NotFound(new { message = outcome.Message }),
                TicketAssignmentError.Forbidden => Results.Forbid(),
                _ => Results.BadRequest(new { message = outcome.Message })
            };
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{UserRoles.Admin},{UserRoles.Manager}" })
        .WithName("AssignTicket")
        .Produces<MaintenanceTicketResponse>(StatusCodes.Status200OK);
    }
}

