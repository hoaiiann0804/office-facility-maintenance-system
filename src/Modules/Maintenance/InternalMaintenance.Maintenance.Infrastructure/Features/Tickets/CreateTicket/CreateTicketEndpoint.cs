using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.CreateTicket;

public sealed class CreateMaintenanceTicketEndpointRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 150 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "EquipmentId must be greater than 0")]
    public int EquipmentId { get; set; }

    [StringLength(20, ErrorMessage = "Priority must not exceed 20 characters")]
    public string Priority { get; set; } = "Medium";

    public int CreatedByUserId { get; set; }
}

/// <summary>
/// [MINIMAL API ENDPOINT - TẠO MỚI PHIẾU BẢO TRÌ]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/tickets
///     -> CreateTicketEndpoint.MapCreateTicketEndpoint(...)
///     -> CreateMaintenanceTicketHandler.Handle(...)
///     -> Trả về HTTP 201 CreatedAtRoute("GetMaintenanceTicketById") hoặc 404/409/400.
/// </summary>
public static class CreateTicketEndpoint
{
    public static RouteHandlerBuilder MapCreateTicketEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("", async (
            [FromBody] CreateMaintenanceTicketEndpointRequest request,
            ClaimsPrincipal user,
            CreateMaintenanceTicketHandler handler,
            CancellationToken cancellationToken) =>
        {
            var outcome = await handler.Handle(
                new CreateMaintenanceTicketCommand(
                    request.Title,
                    request.Description,
                    request.EquipmentId,
                    request.Priority,
                    user.GetRequiredUserId()),
                cancellationToken);

            if (outcome.IsSuccess)
            {
                return Results.CreatedAtRoute(
                    "GetMaintenanceTicketById",
                    new { id = outcome.TicketId },
                    outcome.Response);
            }

            return outcome.Error switch
            {
                CreateMaintenanceTicketError.EquipmentNotFound => Results.NotFound(new { message = outcome.Message }),
                CreateMaintenanceTicketError.DuplicateRace => Results.Conflict(new { message = outcome.Message }),
                _ => Results.BadRequest(new { message = outcome.Message })
            };
        })
        .RequireAuthorization()
        .WithName("CreateMaintenanceTicket")
        .Produces<MaintenanceTicketResponse>(StatusCodes.Status201Created);
    }
}

