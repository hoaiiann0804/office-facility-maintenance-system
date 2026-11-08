using InternalMaintenance.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternalMaintenance.Persistence.Configurations.Maintenance;

// Maintenance mapping nằm cùng nhóm để index trạng thái hoạt động và chi phí có thể đối chiếu với quy tắc ticket.
public sealed class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
    {
        builder.HasIndex(ticket => ticket.TicketCode).IsUnique();
        builder.HasIndex(ticket => ticket.EquipmentId)
            .HasDatabaseName("IX_MaintenanceTickets_EquipmentId_Active")
            .IsUnique()
            // Resolved vẫn chiếm thiết bị tới khi đóng; filtered index phải khớp tập status của domain.
            .HasFilter($"[Status] IN ({string.Join(", ", TicketWorkflowRules.EquipmentOccupyingStatuses.Select(status => $"'{status}'"))})");
    }
}

public sealed class TicketVendorLogConfiguration : IEntityTypeConfiguration<TicketVendorLog>
{
    public void Configure(EntityTypeBuilder<TicketVendorLog> builder)
    {
        builder.HasIndex(log => log.VendorId).HasDatabaseName("IX_TicketVendorLogs_VendorId");
        builder.Property(log => log.RepairCost).HasColumnType("decimal(18,2)");
        builder.Property(log => log.Note).HasMaxLength(1000);
    }
}
