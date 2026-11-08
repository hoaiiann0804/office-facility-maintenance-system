using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketById;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY CHI TIẾT PHIẾU BẢO TRÌ THEO ID]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/tickets/{id:int}
///     -> GetTicketByIdEndpoint.MapGetTicketByIdEndpoint(...)
///     -> Trích xuất Actor (UserId, Role, DepartmentId) từ ClaimsPrincipal
///     -> IMaintenanceTicketReader.GetByIdAsync(...) (điều phối xuống GetTicketByIdHandler)
///     -> Trả về HTTP 200 OK hoặc 404 NotFound.
/// </summary>
public static class GetTicketByIdEndpoint
{
    public static RouteHandlerBuilder MapGetTicketByIdEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("{id:int}", async (
            int id,
            ClaimsPrincipal user,
            IMaintenanceTicketReader ticketReader,
            CancellationToken cancellationToken) =>
        {
            var actor = new MaintenanceTicketActor(
                user.GetRequiredUserId(),
                user.GetRole(),
                user.GetDepartmentId());
            var ticket = await ticketReader.GetByIdAsync(id, actor, cancellationToken);
            return ticket is null
                ? Results.NotFound(new { message = "Ticket not found" })
                : Results.Ok(ticket);
        })
        .RequireAuthorization()
        .WithName("GetMaintenanceTicketById")
        .Produces<MaintenanceTicketResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

