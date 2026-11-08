using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InternalMaintenance.TicketAttachments.Infrastructure;

/// <summary>Chạy processor định kỳ; xử lý thực tế nằm riêng để dễ kiểm thử độc lập với timer.</summary>
public sealed class AttachmentStorageCleanupWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AttachmentStorageCleanupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // BackgroundService là singleton; tạo scope cho processor để dùng DbContext scoped an toàn.
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<AttachmentStorageCleanupProcessor>();
                await processor.ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Lỗi khi xử lý hàng đợi xóa attachment khỏi storage.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
