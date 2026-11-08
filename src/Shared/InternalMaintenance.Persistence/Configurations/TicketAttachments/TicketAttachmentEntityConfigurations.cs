using InternalMaintenance.TicketAttachments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternalMaintenance.Persistence.Configurations.TicketAttachments;

// Attachments sở hữu metadata file; index hỗ trợ tra ticket, chống trùng storage key và dọn object đã xóa.
public sealed class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.HasIndex(attachment => attachment.MaintenanceTicketId);
        builder.HasIndex(attachment => attachment.UploadedByUserId);
        builder.HasIndex(attachment => attachment.StorageKey).IsUnique();
        builder.HasIndex(attachment => attachment.DeletedAt)
            .HasDatabaseName("IX_TicketAttachments_PendingStorageDeletion")
            .HasFilter("[IsDeleted] = 1 AND [StorageDeletedAt] IS NULL");
    }
}

public sealed class TicketAttachmentUploadIntentConfiguration : IEntityTypeConfiguration<TicketAttachmentUploadIntent>
{
    public void Configure(EntityTypeBuilder<TicketAttachmentUploadIntent> builder)
    {
        builder.HasKey(intent => intent.Id);
        builder.Property(intent => intent.StorageKey).HasMaxLength(450).IsRequired();
        builder.Property(intent => intent.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(intent => intent.StoredFileName).HasMaxLength(260).IsRequired();
        builder.Property(intent => intent.ContentType).HasMaxLength(150).IsRequired();
        // Một storage key chỉ thuộc một intent để hai client không xác nhận cùng object upload.
        builder.HasIndex(intent => intent.StorageKey).IsUnique();
        // Worker chỉ quét intent hết hạn chưa xác nhận/chưa dọn, không quét toàn bộ lịch sử upload.
        builder.HasIndex(intent => new { intent.UploadUrlExpiresAt, intent.CleanupLeaseUntil })
            .HasDatabaseName("IX_TicketAttachmentUploadIntents_PendingCleanup")
            .HasFilter("[ConfirmedAt] IS NULL AND [StorageDeletedAt] IS NULL");
    }
}
