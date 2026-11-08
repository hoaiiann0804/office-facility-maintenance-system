using InternalMaintenance.Assets.Domain;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Organization.Domain;
using InternalMaintenance.TicketAttachments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternalMaintenance.Persistence.Configurations;

// Persistence giữ một số FK vật lý giữa các module để SQL Server chặn ID mồ côi.
// FK là ràng buộc của database dùng chung, không phải lý do để module import entity/service của module khác.
// Chỉ gỡ FK sau khi xác định rõ cách giữ audit và xác thực ID ở mọi đường ghi; user hiện được vô hiệu hóa,
// không hard-delete, nên các FK tới User còn giữ lịch sử tham chiếu hợp lệ.
public sealed class MaintenanceTicketRelationships : IEntityTypeConfiguration<MaintenanceTicket>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
    {
        // Ticket lưu ID người tạo và kỹ thuật viên; Restrict buộc xử lý lịch sử ticket trước khi xóa User.
        builder.HasOne<User>().WithMany().HasForeignKey(ticket => ticket.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(ticket => ticket.AssignedTechnicianId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(ticket => ticket.SupportTechnicianId).OnDelete(DeleteBehavior.Restrict);

        // Equipment thuộc Assets; giữ FK ở database nhưng không tạo navigation chéo trong entity Maintenance.
        builder.HasOne<Equipment>().WithMany().HasForeignKey(ticket => ticket.EquipmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TicketStatusHistoryRelationships : IEntityTypeConfiguration<TicketStatusHistory>
{
    public void Configure(EntityTypeBuilder<TicketStatusHistory> builder)
    {
        // Audit phải còn nguyên để biết ai đã đổi trạng thái, vì vậy không cascade khi User bị xóa.
        builder.HasOne<User>().WithMany().HasForeignKey(history => history.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EquipmentRelationships : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        // Assets chỉ giữ DepartmentId; FK vẫn ngăn xóa phòng ban đang được thiết bị sử dụng.
        builder.HasOne<Department>().WithMany().HasForeignKey(equipment => equipment.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(equipment => equipment.MaintenanceDepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserRelationships : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Role là dữ liệu sở hữu của Identity nên navigation nội bộ được giữ; Department chỉ được tham chiếu bằng ID.
        builder.HasOne(user => user.Role).WithMany(role => role.Users).HasForeignKey(user => user.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(user => user.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TicketCommentRelationships : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        // Comment thuộc ticket nên xóa ticket bị chặn khi còn comment; tác giả được lưu bằng UserId, không có navigation chéo.
        builder.HasOne(comment => comment.MaintenanceTicket).WithMany(ticket => ticket.Comments)
            .HasForeignKey(comment => comment.MaintenanceTicketId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(comment => comment.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TicketAttachmentRelationships : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        // Service đã kiểm tra ticket/quyền qua Maintenance contract; FK Restrict vẫn cần để chặn
        // hard-delete ngoài luồng API làm metadata và object trong storage trở thành file mồ côi.
        builder.HasOne<MaintenanceTicket>().WithMany().HasForeignKey(attachment => attachment.MaintenanceTicketId).OnDelete(DeleteBehavior.Restrict);

        // Identity hiện vô hiệu hóa User thay vì xóa cứng. Giữ FK giúp UploadedByUserId trong audit
        // luôn trỏ tới một tài khoản đã từng tồn tại, dù tài khoản đó sau này không còn hoạt động.
        builder.HasOne<User>().WithMany().HasForeignKey(attachment => attachment.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RefreshTokenRelationships : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Refresh token không có ý nghĩa sau khi tài khoản bị xóa nên cascade là hành vi chủ đích của Identity.
        builder.HasOne(token => token.User).WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TicketVendorLogRelationships : IEntityTypeConfiguration<TicketVendorLog>
{
    public void Configure(EntityTypeBuilder<TicketVendorLog> builder)
    {
        // Log thuộc vòng đời ticket nên được xóa cùng ticket. Vendor là module khác, vì vậy
        // chỉ giữ VendorId và dùng IVendorDirectory để xác thực; không tạo FK chéo trong schema.
        builder.HasOne(log => log.MaintenanceTicket).WithMany(ticket => ticket.VendorLogs)
            .HasForeignKey(log => log.MaintenanceTicketId).OnDelete(DeleteBehavior.Cascade);
    }
}
