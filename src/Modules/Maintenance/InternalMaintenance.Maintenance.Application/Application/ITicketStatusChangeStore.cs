using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

/// <summary>
/// Cổng persistence của use case. Interface cho phép use case yêu cầu dữ liệu/lưu thay đổi
/// mà không tự phụ thuộc vào EF Core hay cách viết SQL query.
/// </summary>
public interface ITicketStatusChangeStore
{
    Task<MaintenanceTicket?> GetForStatusChangeAsync(int ticketId, CancellationToken cancellationToken);
    Task<TicketVendorLog?> GetOpenVendorLogAsync(int ticketId, CancellationToken cancellationToken);
    void AddVendorLog(TicketVendorLog vendorLog);
    void AddStatusHistory(TicketStatusHistory history);
    // Handler gọi đúng một lần sau khi gateway đã cập nhật Equipment. Adapter Equipment không
    // được tự SaveChanges; nếu mỗi module commit riêng, lỗi giữa chừng sẽ làm lệch trạng thái.
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken);
}
