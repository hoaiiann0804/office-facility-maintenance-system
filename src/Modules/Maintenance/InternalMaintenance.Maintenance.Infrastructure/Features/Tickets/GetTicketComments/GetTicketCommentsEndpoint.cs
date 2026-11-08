using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketComments;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY DANH SÁCH BÌNH LUẬN CỦA TICKET]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/tickets/{id:int}/comments
///     -> GetTicketCommentsEndpoint.MapGetTicketCommentsEndpoint(...)
///     -> IMaintenanceTicketReader.GetCommentsAsync(...) (điều phối xuống GetTicketCommentsHandler)
///     -> Trả về HTTP 200 OK hoặc 404 NotFound.
/// </summary>
public static class GetTicketCommentsEndpoint
{
    public static RouteHandlerBuilder MapGetTicketCommentsEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("{id:int}/comments", async (
            int id,
            ClaimsPrincipal user,
            IMaintenanceTicketReader ticketReader,
            CancellationToken cancellationToken) =>
        {
            var actor = new MaintenanceTicketActor(
                user.GetRequiredUserId(),
                user.GetRole(),
                user.GetDepartmentId());
            var comments = await ticketReader.GetCommentsAsync(id, actor, cancellationToken);
            return comments is null
                ? Results.NotFound(new { message = "Ticket not found" })
                : Results.Ok(comments);
        })
        .RequireAuthorization()
        .WithName("GetCommentById")
        .Produces<IReadOnlyList<TicketCommentResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

