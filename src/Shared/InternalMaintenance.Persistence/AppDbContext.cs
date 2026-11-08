using InternalMaintenance.Assets.Domain;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Organization.Domain;
using InternalMaintenance.TicketAttachments.Domain;
using InternalMaintenance.Vendors.Domain;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Persistence;

// Namespace khớp với assembly Persistence để module Infrastructure không phải import tên giả thuộc API host.
 
/// <summary>
/// Unit of work SQL dùng chung của modular monolith. Mỗi request nhận một instance scoped;
/// do đó các thay đổi ticket, equipment, history và vendor log có thể commit/rollback cùng nhau.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketAttachmentUploadIntent> TicketAttachmentUploadIntents => Set<TicketAttachmentUploadIntent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<TicketVendorLog> TicketVendorLogs => Set<TicketVendorLog>();
    public DbSet<SlaNotificationOutboxMessage> SlaNotificationOutboxMessages => Set<SlaNotificationOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Context và các mapping chuyển cùng assembly để EF không bỏ sót cấu hình module khi host tách project.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
