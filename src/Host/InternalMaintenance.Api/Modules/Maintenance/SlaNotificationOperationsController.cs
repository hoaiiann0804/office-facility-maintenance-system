using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.Maintenance;

[ApiController]
[Route("api/maintenance/sla-notifications")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
public sealed class SlaNotificationOperationsController(
    ISlaOutboxFailureReader failureReader,
    ISlaOutboxRetryService retryService) : ControllerBase
{
    /// <summary>
    /// Trả về email SLA đã dừng retry để Admin/Manager kiểm tra địa chỉ nhận hoặc cấu hình Resend.
    /// </summary>
    [HttpGet("failed")]
    public async Task<ActionResult<IReadOnlyList<SlaOutboxFailure>>> GetFailedNotifications(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Giới hạn request giúp endpoint vận hành ổn định kể cả khi database có nhiều lỗi tích lũy.
        var failures = await failureReader.GetRecentFailuresAsync(
            Math.Clamp(limit, 1, 100), cancellationToken);

        return Ok(failures);
    }

    /// <summary>Đưa email thất bại về hàng chờ sau khi Admin đã sửa cấu hình hoặc dữ liệu nhận.</summary>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("failed/{outboxId:long}/retry")]
    public async Task<IActionResult> RetryFailedNotification(long outboxId, CancellationToken cancellationToken)
    {
        var requeued = await retryService.RequeueFailedAsync(outboxId, cancellationToken);
        if (!requeued)
            return NotFound(new { message = "Không tìm thấy thông báo đang ở trạng thái thất bại để gửi lại." });

        // Worker sẽ nhận message ở chu kỳ quét tiếp theo; endpoint không gửi email đồng bộ trong request.
        return Accepted(new { message = "Thông báo đã được đưa lại vào hàng đợi gửi." });
    }
}
