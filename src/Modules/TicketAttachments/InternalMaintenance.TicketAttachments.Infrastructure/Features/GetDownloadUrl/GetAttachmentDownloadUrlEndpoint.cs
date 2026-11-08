using InternalMaintenance.TicketAttachments.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.GetDownloadUrl;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY PRESIGNED DOWNLOAD URL]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/tickets/{ticketId:int}/attachments/{attachmentId:int}/download-url
///     -> GetAttachmentDownloadUrlEndpoint.MapGetAttachmentDownloadUrlEndpoint(...)
///     -> ITicketAttachmentsService.GetDownloadUrlAsync(...) (điều phối xuống GetAttachmentDownloadUrlHandler)
///     -> Trả về HTTP 200 OK kèm chuỗi URL tải xuống tạm thời.
/// </summary>
public static class GetAttachmentDownloadUrlEndpoint
{
    public static RouteHandlerBuilder MapGetAttachmentDownloadUrlEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("{attachmentId:int}/download-url", async (
            int ticketId,
            int attachmentId,
            ITicketAttachmentsService ticketAttachmentsService) =>
        {
            var result = await ticketAttachmentsService.GetDownloadUrlAsync(ticketId, attachmentId);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .RequireAuthorization()
        .WithName("GetAttachmentDownloadUrl")
        .Produces<string>(StatusCodes.Status200OK);
    }
}

