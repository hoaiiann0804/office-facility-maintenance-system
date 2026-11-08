using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketHistory;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY LỊCH SỬ TRẠNG THÁI CỦA TICKET]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/tickets/{id:int}/history
///     -> GetTicketHistoryEndpoint.MapGetTicketHistoryEndpoint(...)
///     -> IMaintenanceTicketReader.GetHistoryAsync(...) (điều phối xuống GetTicketHistoryHandler)
///     -> Trả về HTTP 200 OK hoặc 400 BadRequest (khi không tìm thấy hoặc không có quyền).
/// </summary>
public static class GetTicketHistoryEndpoint
{
    public static RouteHandlerBuilder MapGetTicketHistoryEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("{id:int}/history", async (
            int id,
            ClaimsPrincipal user,
            IMaintenanceTicketReader ticketReader,
            CancellationToken cancellationToken) =>
        {
            var actor = new MaintenanceTicketActor(
                user.GetRequiredUserId(),
                user.GetRole(),
                user.GetDepartmentId());
            var histories = await ticketReader.GetHistoryAsync(id, actor, cancellationToken);
            return histories is null
                ? Results.BadRequest(new { message = "Ticket not found" })
                : Results.Ok(histories);
        })
        .RequireAuthorization()
        .WithName("GetTicketHistory")
        .Produces<IReadOnlyList<TicketStatusHistoryResponse>>(StatusCodes.Status200OK);
    }
}

