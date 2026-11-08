using InternalMaintenance.TicketAttachments.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.TicketAttachments.Infrastructure.Features.DeleteAttachment;

/// <summary>
/// [MINIMAL API ENDPOINT - XÓA MỀM ATTACHMENT &amp; ĐƯA VÀO HÀNG ĐỢI DỌN STORAGE]
///
/// - Luồng đi (Request Flow):
///   HTTP DELETE /api/tickets/{ticketId:int}/attachments/{attachmentId:int}
///     -> DeleteAttachmentEndpoint.MapDeleteAttachmentEndpoint(...)
///     -> ITicketAttachmentsService.DeleteAsync(...) (điều phối xuống DeleteAttachmentHandler)
///     -> Trả về HTTP 204 NoContent hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class DeleteAttachmentEndpoint
{
    public static RouteHandlerBuilder MapDeleteAttachmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapDelete("{attachmentId:int}", async (
            int ticketId,
            int attachmentId,
            ITicketAttachmentsService ticketAttachmentsService) =>
        {
            var result = await ticketAttachmentsService.DeleteAsync(ticketId, attachmentId);
            return result.IsSuccess
                ? Results.StatusCode(result.StatusCode)
                : Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
        })
        .RequireAuthorization()
        .WithName("DeleteAttachment")
        .Produces(StatusCodes.Status204NoContent);
    }
}

