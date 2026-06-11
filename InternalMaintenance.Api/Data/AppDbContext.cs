using InternalMaintenance.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace InternalMaintenance.Api.Data;
// DbContext = lớp của EF Core dùng để làm việc với database
// AppDbContext = lớp đại diện cho database trong code C#
public class AppDbContext : DbContext
{
    //Nhận cấu hình database từ Program.cs
    public AppDbContext (DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    //Các bảng chính trong Database 
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasIndex(role=>role.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(user=>user.Email).IsUnique();
        modelBuilder.Entity<Equipment>().HasIndex(equipment=>equipment.Code).IsUnique();
        modelBuilder.Entity<MaintenanceTicket>().HasIndex(ticket=>ticket.TicketCode).IsUnique();

        // 1 User có thể tạo nhiều Ticket
        modelBuilder.Entity<MaintenanceTicket>()
            .HasOne(ticket => ticket.CreatedByUser)
            .WithMany(user => user.CreatedTickets)
            .HasForeignKey(ticket => ticket.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        // 1 Techincian có thể được giao nhiều ticket 
        modelBuilder.Entity<MaintenanceTicket>()
            .HasOne(ticket => ticket.AssignedTechnician)
            .WithMany(user => user.AssignedTickets)
            .HasForeignKey(user => user.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1 User có thể thực hiện nhiều lần đổi trạng thái ticket
        modelBuilder.Entity<TicketStatusHistory>()
            .HasOne(history => history.ChangedByUser)
            .WithMany(user => user.TicketStatusHistories)
            .HasForeignKey(user => user.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
