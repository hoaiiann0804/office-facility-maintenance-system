using InternalMaintenance.TicketAttachments.Infrastructure.Features.ConfirmAttachment;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.CreatePresign;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.DeleteAttachment;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.GetDownloadUrl;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.GetTicketAttachments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.TicketAttachments.Infrastructure.DependencyInjection;

/// <summary>
/// [MODULE ENDPOINT REGISTRATION - TICKET ATTACHMENTS]
/// Gom toàn bộ 5 Minimal API Endpoints của các Vertical Slices trong module TicketAttachments
/// vào chung một Route Group `/api/tickets/{ticketId:int}/attachments` theo chuẩn FullStackHero.
/// </summary>
public static class TicketAttachmentEndpoints
{
    public static IEndpointRouteBuilder MapTicketAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/tickets/{ticketId:int}/attachments")
            .WithTags("TicketAttachments")
            .RequireAuthorization();

        group.MapCreatePresignAttachmentEndpoint();
        group.MapConfirmAttachmentEndpoint();
        group.MapGetTicketAttachmentsEndpoint();
        group.MapGetAttachmentDownloadUrlEndpoint();
        group.MapDeleteAttachmentEndpoint();

        return app;
    }
}

