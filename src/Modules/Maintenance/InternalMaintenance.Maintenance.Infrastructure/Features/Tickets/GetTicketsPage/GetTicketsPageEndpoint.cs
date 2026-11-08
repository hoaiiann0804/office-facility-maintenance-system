using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketsPage;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY DANH SÁCH PHIẾU BẢO TRÌ CÓ PHÂN TRANG]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/tickets?page=...&amp;pageSize=...&amp;status=...&amp;priority=...&amp;equipmentId=...
///     -> GetTicketsPageEndpoint.MapGetTicketsPageEndpoint(...)
///     -> Trích xuất Actor (UserId, Role, DepartmentId) từ ClaimsPrincipal
///     -> IMaintenanceTicketReader.GetPageAsync(...) (điều phối xuống GetTicketsPageHandler)
///     -> Trả về HTTP 200 OK kèm PagedResponse&lt;MaintenanceTicketResponse&gt;.
/// </summary>
public static class GetTicketsPageEndpoint
{
    public static RouteHandlerBuilder MapGetTicketsPageEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("", async (
            [AsParameters] TicketQuery query,
            ClaimsPrincipal user,
            IMaintenanceTicketReader ticketReader,
            CancellationToken cancellationToken) =>
        {
            var actor = new MaintenanceTicketActor(
                user.GetRequiredUserId(),
                user.GetRole(),
                user.GetDepartmentId());
            var page = await ticketReader.GetPageAsync(query, actor, cancellationToken);
            return Results.Ok(page);
        })
        .RequireAuthorization()
        .WithName("GetMaintenanceTickets")
        .Produces<PagedResponse<MaintenanceTicketResponse>>(StatusCodes.Status200OK);
    }
}

