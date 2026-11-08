using InternalMaintenance.TicketAttachments.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.CreatePresign;

/// <summary>
/// [MINIMAL API ENDPOINT - TẠO PRESIGNED UPLOAD URL]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/tickets/{ticketId:int}/attachments/presign
///     -> CreatePresignAttachmentEndpoint.MapCreatePresignAttachmentEndpoint(...)
///     -> ITicketAttachmentsService.CreatePresignAsync(...) (điều phối xuống CreatePresignAttachmentHandler)
///     -> Trả về HTTP 200 OK kèm PresignAttachmentResponse.
/// </summary>
public static class CreatePresignAttachmentEndpoint
{
    public static RouteHandlerBuilder MapCreatePresignAttachmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("presign", async (
            int ticketId,
            [FromBody] PresignAttachmentRequest request,
            ITicketAttachmentsService ticketAttachmentsService) =>
        {
            var result = await ticketAttachmentsService.CreatePresignAsync(ticketId, request);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .RequireAuthorization()
        .WithName("CreatePresignAttachment")
        .Produces<PresignAttachmentResponse>(StatusCodes.Status200OK);
    }
}

