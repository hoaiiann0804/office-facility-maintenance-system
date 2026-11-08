using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InternalMaintenance.Maintenance.Infrastructure;

public class SlaMonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaMonitorWorker> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private const int MaxDeliveryAttempts = 10;

    public SlaMonitorWorker(IServiceScopeFactory scopeFactory, ILogger<SlaMonitorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitor Worker initialized. Polling interval: {IntervalMinutes} minute(s)", CheckInterval.TotalMinutes);
        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSlaChecksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred during SLA monitoring cycle");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("SLA Monitor Worker has stopped gracefully.");
    }

    private async Task ProcessSlaChecksAsync(CancellationToken stoppingToken)
    {
        // Worker chạy singleton nhưng DbContext và IEmailService là scoped: tạo một scope cho mỗi lượt
        // quét để cả hai sống đủ lâu trong khi xử lý, rồi được dispose trước lượt kế tiếp.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var now = DateTime.UtcNow;

        // Resolved đã chốt MetSLA/MissedSLA, còn WaitingForVendor đã dừng đồng hồ.
        // Chỉ lấy các status mà SLA thực sự đang chạy để tránh ghi đè kết quả đã chốt.
        var tickets = await db.MaintenanceTickets
            .AsNoTracking()
            .Where(t => t.DueAt != null)
            .Where(t => TicketWorkflowRules.SlaTrackedStatuses.Contains(t.Status))
            .ToListAsync(stoppingToken);

        foreach (var ticket in tickets)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await EvaluateTicketSlaAsync(ticket, db, now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SLA evaluation for ticket #{TicketCode} (ID: {TicketId})",
                    ticket.TicketCode, ticket.Id);
            }
        }

        // Xử lý outbox sau khi quét SLA: email chỉ được gửi khi thông báo đã commit bền vững.
        await DispatchPendingNotificationsAsync(db, emailService, stoppingToken);
    }

    private async Task EvaluateTicketSlaAsync(
        MaintenanceTicket ticket,
        AppDbContext db,
        DateTime now,
        CancellationToken stoppingToken)
    {
        if (!ticket.DueAt.HasValue) return;

        // Aggregate quyết định loại cảnh báo theo workflow/SLA. Worker ghi claim và outbox
        // trong cùng transaction; nếu process dừng giữa chừng thì không mất email đã claim.
        var observedDueAt = ticket.DueAt.Value;
        foreach (var notification in ticket.GetDueSlaNotifications(now))
        {
            if (!await QueueNotificationAsync(db, ticket.Id, observedDueAt, notification, now, stoppingToken))
                continue;

            ticket.MarkSlaNotificationClaimed(notification, now);
            switch (notification)
            {
                case SlaNotificationType.NearBreach:
                    _logger.LogInformation("Ticket #{TicketCode} is near SLA breach (Due: {DueAt}). State persisted.",
                        ticket.TicketCode, observedDueAt);
                    break;
                case SlaNotificationType.Breached:
                    _logger.LogWarning("Ticket #{TicketCode} breached SLA (Due: {DueAt}, Now: {Now}). State persisted.",
                        ticket.TicketCode, observedDueAt, now);
                    break;
                case SlaNotificationType.Escalated:
                    _logger.LogWarning("Ticket #{TicketCode} escalated due to severe breach (>2h overdue). State persisted.",
                        ticket.TicketCode);
                    break;
            }
        }
    }

    private static async Task<bool> QueueNotificationAsync(
        AppDbContext db,
        int ticketId,
        DateTime observedDueAt,
        SlaNotificationType notification,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var query = db.MaintenanceTickets
            .Where(t => t.Id == ticketId)
            .Where(t => TicketWorkflowRules.SlaTrackedStatuses.Contains(t.Status))
            .Where(t => t.DueAt == observedDueAt);

        // Điều kiện được xác nhận lại ở database. Hai worker cùng đọc một row thì
        // chỉ worker đầu tiên chuyển NotifiedAt từ NULL sang timestamp.
        var affected = notification switch
        {
            SlaNotificationType.NearBreach => await query
                // Giờ gần vi phạm là khoảng thời gian từ bây giờ đến hạn SLA.
                // Dùng điều kiện này cùng với điều kiện của domain để tránh claim nhầm ticket đã quá hạn.
                .Where(t => t.DueAt > now && t.DueAt <= now.AddHours(1))
                .Where(t => t.NearBreachNotifiedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.SlaStatus, SlaPolicy.NearBreach)
                    .SetProperty(t => t.NearBreachNotifiedAt, now), cancellationToken),
            SlaNotificationType.Breached => await query
                .Where(t => t.DueAt < now)
                .Where(t => t.BreachedNotifiedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.SlaStatus, SlaPolicy.Breached)
                    .SetProperty(t => t.BreachedNotifiedAt, now), cancellationToken),
            SlaNotificationType.Escalated => await query
                .Where(t => t.DueAt < now.AddHours(-2))
                .Where(t => t.EscalatedNotifiedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.EscalatedNotifiedAt, now), cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(notification))
        };

        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // Ticket claim + message được commit cùng nhau: không còn trạng thái “đã claim nhưng
        // chưa kịp gửi thì process chết” làm mất cảnh báo. Unique index chặn enqueue trùng.
        db.SlaNotificationOutboxMessages.Add(new SlaNotificationOutboxMessage
        {
            MaintenanceTicketId = ticketId,
            DueAt = observedDueAt,
            NotificationType = notification,
            QueuedAt = now,
            NextAttemptAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task DispatchPendingNotificationsAsync(
        AppDbContext db,
        IEmailService emailService,
        CancellationToken stoppingToken)
    {
        // Lease có hạn để instance khác có thể tiếp quản nếu worker bị kill khi đang gửi.
        var now = DateTime.UtcNow;
        var candidateIds = await db.SlaNotificationOutboxMessages
            .Where(message => message.ProcessedAt == null && message.FailedAt == null && message.NextAttemptAt <= now)
            .Where(message => message.LeaseUntil == null || message.LeaseUntil <= now)
            .OrderBy(message => message.NextAttemptAt)
            .Select(message => message.Id)
            .Take(50)
            .ToListAsync(stoppingToken);

        foreach (var id in candidateIds)
        {
            var leaseId = Guid.NewGuid();
            var claimedAt = DateTime.UtcNow;
            var claimed = await db.SlaNotificationOutboxMessages
                .Where(message => message.Id == id && message.ProcessedAt == null && message.FailedAt == null)
                .Where(message => message.NextAttemptAt <= claimedAt)
                .Where(message => message.LeaseUntil == null || message.LeaseUntil <= claimedAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.LeaseId, leaseId)
                    .SetProperty(message => message.LeaseUntil, claimedAt.AddMinutes(10))
                    .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1),
                    stoppingToken);
            if (claimed == 0) continue; // Instance khác đã lấy lease trước.

            var message = await db.SlaNotificationOutboxMessages
                .AsNoTracking()
                .SingleAsync(item => item.Id == id && item.LeaseId == leaseId, stoppingToken);
            var ticket = await db.MaintenanceTickets.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == message.MaintenanceTicketId, stoppingToken);

            if (ticket is null)
            {
                // FK cascade thường loại trường hợp này; đánh dấu hoàn tất để không retry vô hạn
                // nếu dữ liệu cũ/đã import tạo ra message không còn ticket tương ứng.
                await CompleteMessageAsync(db, id, leaseId, DateTime.UtcNow, stoppingToken);
                continue;
            }

            try
            {
                await SendAsync(emailService, ticket, message.NotificationType, message.Id, message.QueuedAt, stoppingToken);
                await CompleteMessageAsync(db, id, leaseId, DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw; // Lease tự hết hạn để worker lần sau gửi tiếp.
            }
            catch (PermanentEmailDeliveryException ex)
            {
                await MarkFailedAsync(db, id, leaseId, ex.Message, DateTime.UtcNow, stoppingToken);
                _logger.LogError(ex,
                    "SLA email {OutboxId} cannot be retried; it was marked failed for operator follow-up", id);
            }
            catch (Exception ex)
            {
                var failedAt = DateTime.UtcNow;
                // Database chỉ cần thông điệp ngắn cho endpoint quản trị; stack trace đầy đủ ở log server.
                var error = ex.Message;
                if (error.Length > 2000) error = error[..2000];

                if (message.AttemptCount >= MaxDeliveryAttempts)
                {
                    // Retry có giới hạn để sự cố kéo dài không tạo hàng đợi vô hạn; giữ bản ghi lỗi để điều tra.
                    await MarkFailedAsync(db, id, leaseId, error, failedAt, stoppingToken);
                    _logger.LogError(ex,
                        "SLA email {OutboxId} reached the {MaxAttempts}-attempt limit and was marked failed",
                        id, MaxDeliveryAttempts);
                    continue;
                }

                var retryAt = failedAt.AddMinutes(Math.Min(60, Math.Pow(2, message.AttemptCount)));

                // Retry bền vững với exponential backoff, giới hạn tối đa 60 phút để tránh
                // spam nhà cung cấp email khi dịch vụ đang lỗi kéo dài.
                await db.SlaNotificationOutboxMessages
                    .Where(item => item.Id == id && item.LeaseId == leaseId && item.ProcessedAt == null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.LastError, error)
                        .SetProperty(item => item.NextAttemptAt, retryAt)
                        .SetProperty(item => item.LeaseId, (Guid?)null)
                        .SetProperty(item => item.LeaseUntil, (DateTime?)null), stoppingToken);
                _logger.LogError(ex,
                    "Email delivery failed for SLA message {OutboxId}; retry scheduled at {RetryAt}", id, retryAt);
            }
        }
    }

    private static Task<int> CompleteMessageAsync(
        AppDbContext db, long id, Guid leaseId, DateTime processedAt, CancellationToken cancellationToken) =>
        db.SlaNotificationOutboxMessages
            .Where(item => item.Id == id && item.LeaseId == leaseId && item.ProcessedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.ProcessedAt, processedAt)
                .SetProperty(item => item.LastError, (string?)null)
                .SetProperty(item => item.LeaseId, (Guid?)null)
                .SetProperty(item => item.LeaseUntil, (DateTime?)null), cancellationToken);

    private static Task<int> MarkFailedAsync(
        AppDbContext db, long id, Guid leaseId, string error, DateTime failedAt, CancellationToken cancellationToken) =>
        db.SlaNotificationOutboxMessages
            .Where(item => item.Id == id && item.LeaseId == leaseId && item.ProcessedAt == null)
            .ExecuteUpdateAsync(setters => setters
                // FailedAt giữ lại trạng thái cuối để có thể lọc khỏi hàng đợi nhưng vẫn truy vết lỗi trong DB.
                .SetProperty(item => item.FailedAt, failedAt)
                .SetProperty(item => item.LastError, error.Length > 2000 ? error[..2000] : error)
                .SetProperty(item => item.LeaseId, (Guid?)null)
                .SetProperty(item => item.LeaseUntil, (DateTime?)null), cancellationToken);

    private static Task SendAsync(
        IEmailService emailService,
        MaintenanceTicket ticket,
        SlaNotificationType notification,
        long outboxId,
        DateTime queuedAt,
        CancellationToken cancellationToken)
    {
        // Khóa ổn định theo outbox message giúp Resend loại email trùng khi provider đã nhận request
        // nhưng worker chưa kịp ghi ProcessedAt hoặc client bị timeout khi chờ phản hồi. Resend giữ khóa
        // trong 24 giờ; QueuedAt chỉ đổi khi Admin chủ động gửi lại, do đó lượt mới có khóa riêng.
        var idempotencyKey = $"maintenance-sla-outbox-{outboxId}-{queuedAt.Ticks}";

        return notification switch
        {
            SlaNotificationType.NearBreach => emailService.SendNearBreachAlertAsync(ticket, cancellationToken, idempotencyKey),
            SlaNotificationType.Breached => emailService.SendBreachedAlertAsync(ticket, cancellationToken, idempotencyKey),
            SlaNotificationType.Escalated => emailService.SendEscalationAlertAsync(ticket, cancellationToken, idempotencyKey),
            _ => throw new ArgumentOutOfRangeException(nameof(notification))
        };
    }
}
