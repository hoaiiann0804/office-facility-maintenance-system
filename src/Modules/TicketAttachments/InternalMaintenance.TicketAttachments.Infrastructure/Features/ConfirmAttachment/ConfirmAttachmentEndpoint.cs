using InternalMaintenance.TicketAttachments.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.ConfirmAttachment;

/// <summary>
/// [MINIMAL API ENDPOINT - XÁC NHẬN FILE ĐÃ UPLOAD LÊN CLOUDFLARE R2]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/tickets/{ticketId:int}/attachments/confirm
///     -> ConfirmAttachmentEndpoint.MapConfirmAttachmentEndpoint(...)
///     -> ITicketAttachmentsService.ConfirmAsync(...) (điều phối xuống ConfirmAttachmentHandler)
///     -> Trả về HTTP StatusCode(result.StatusCode) kèm TicketAttachmentResponse.
/// </summary>
public static class ConfirmAttachmentEndpoint
{
    public static RouteHandlerBuilder MapConfirmAttachmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("confirm", async (
            int ticketId,
            [FromBody] ConfirmAttachmentRequest request,
            ITicketAttachmentsService ticketAttachmentsService) =>
        {
            var result = await ticketAttachmentsService.ConfirmAsync(ticketId, request);
            return result.IsSuccess
                ? Results.Json(result.Data, statusCode: result.StatusCode)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .RequireAuthorization()
        .WithName("ConfirmAttachment")
        .Produces<TicketAttachmentResponse>(StatusCodes.Status201Created);
    }
}

