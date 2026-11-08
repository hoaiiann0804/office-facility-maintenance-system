using InternalMaintenance.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternalMaintenance.Persistence.Configurations.Maintenance;

public sealed class SlaNotificationOutboxConfiguration : IEntityTypeConfiguration<SlaNotificationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<SlaNotificationOutboxMessage> builder)
    {
        builder.HasKey(message => message.Id);
        builder.Property(message => message.LastError).HasMaxLength(2000);
        // DueAt thuộc khóa logic để priority/pause tạo chu kỳ SLA mới mà không gộp nhầm notification cũ.
        builder.HasIndex(message => new { message.MaintenanceTicketId, message.DueAt, message.NotificationType })
            .IsUnique();
        // Worker chỉ quét message chờ xử lý; message thất bại vĩnh viễn vẫn được giữ để audit.
        builder.HasIndex(message => new { message.NextAttemptAt, message.LeaseUntil })
            .HasDatabaseName("IX_SlaNotificationOutbox_Pending")
            .HasFilter("[ProcessedAt] IS NULL AND [FailedAt] IS NULL");
        builder.HasOne<MaintenanceTicket>()
            .WithMany()
            .HasForeignKey(message => message.MaintenanceTicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
