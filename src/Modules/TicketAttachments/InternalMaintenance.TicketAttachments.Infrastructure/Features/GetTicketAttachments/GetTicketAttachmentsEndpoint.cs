using InternalMaintenance.TicketAttachments.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.GetTicketAttachments;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY DANH SÁCH FILE ĐÍNH KÈM CỦA TICKET]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/tickets/{ticketId:int}/attachments
///     -> GetTicketAttachmentsEndpoint.MapGetTicketAttachmentsEndpoint(...)
///     -> ITicketAttachmentsService.GetByTicketIdAsync(...) (điều phối xuống GetTicketAttachmentsHandler)
///     -> Trả về HTTP 200 OK kèm List&lt;TicketAttachmentResponse&gt;.
/// </summary>
public static class GetTicketAttachmentsEndpoint
{
    public static RouteHandlerBuilder MapGetTicketAttachmentsEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("", async (
            int ticketId,
            ITicketAttachmentsService ticketAttachmentsService) =>
        {
            var result = await ticketAttachmentsService.GetByTicketIdAsync(ticketId);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .RequireAuthorization()
        .WithName("GetTicketAttachments")
        .Produces<List<TicketAttachmentResponse>>(StatusCodes.Status200OK);
    }
}

