using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.SlaOutbox;

/// <summary>
/// [MINIMAL API ENDPOINT - QUẢN TRỊ HÀNG ĐỢI GỬI EMAIL SLA OUTBOX]
///
/// - Luồng đi (Request Flow):
///   1. HTTP GET /api/maintenance/sla-notifications/failed?limit=...
///      -> ISlaOutboxFailureReader.GetRecentFailuresAsync(...)
///   2. HTTP POST /api/maintenance/sla-notifications/failed/{outboxId:long}/retry
///      -> ISlaOutboxRetryService.RequeueFailedAsync(...)
/// </summary>
public static class SlaNotificationOperationsEndpoints
{
    public static IEndpointRouteBuilder MapSlaNotificationOperationsEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("failed", async (
            [FromQuery] int? limit,
            ISlaOutboxFailureReader failureReader,
            CancellationToken cancellationToken) =>
        {
            var safeLimit = Math.Clamp(limit ?? 50, 1, 100);
            var failures = await failureReader.GetRecentFailuresAsync(safeLimit, cancellationToken);
            return Results.Ok(failures);
        })
        .WithName("GetFailedSlaNotifications")
        .Produces<IReadOnlyList<SlaOutboxFailure>>(StatusCodes.Status200OK);

        group.MapPost("failed/{outboxId:long}/retry", async (
            long outboxId,
            ISlaOutboxRetryService retryService,
            CancellationToken cancellationToken) =>
        {
            var requeued = await retryService.RequeueFailedAsync(outboxId, cancellationToken);
            if (!requeued)
                return Results.NotFound(new { message = "Không tìm thấy thông báo đang ở trạng thái thất bại để gửi lại." });

            return Results.Accepted(value: new { message = "Thông báo đã được đưa lại vào hàng đợi gửi." });
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = UserRoles.Admin })
        .WithName("RetryFailedSlaNotification");

        return group;
    }
}

