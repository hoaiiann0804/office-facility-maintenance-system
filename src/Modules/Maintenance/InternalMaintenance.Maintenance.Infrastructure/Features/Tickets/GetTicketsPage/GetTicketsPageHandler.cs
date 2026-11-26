using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.Common;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketsPage;

/// <summary>
/// [VERTICAL SLICE: GET TICKETS PAGE - TRUY VẤN DANH SÁCH PHIẾU BẢO TRÌ PHÂN TRANG CÓ PHÂN QUYỀN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/tickets (MaintenanceTicketsController)
///           -> IMaintenanceTicketReader.GetPageAsync(query, actor)
///           -> GetTicketsPageHandler.HandleAsync(query, actor)
///           -> Bước 1: Áp dụng bộ lọc tầm nhìn theo vai trò (`TicketVisibilityFilter.ApplyVisibilityAsync`).
///           -> Bước 2: Lọc theo `Status`, `Priority`, `EquipmentId`, `Keyword` và `SlaStatus` trên `IQueryable&lt;MaintenanceTicket&gt;`.
///           -> Bước 3: Đếm tổng số bản ghi (`CountAsync`) + Phân trang (`ApplyPaging`) + Ánh xạ SQL (`ResponseProjection`).
///           -> Bước 4: Gọi Batch Lookup (`PopulateAllExternalDetailsAsync`) sang 3 modules (`Identity`, `Equipment`, `Vendors`)
///              để điền tên người tạo/kỹ thuật viên, mã/tên thiết bị và tên đối tác sửa chữa.
/// </summary>
public sealed class GetTicketsPageHandler(
    AppDbContext db,
    TicketVisibilityFilter visibilityFilter,
    IIdentityUserDirectory identityDirectory,
    IEquipmentTicketContextReader equipmentReader,
    IVendorDirectory vendorDirectory)
{
    public async Task<PagedResponse<MaintenanceTicketResponse>> HandleAsync(
        TicketQuery query,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default)
    {
        var visibleTickets = await visibilityFilter.ApplyVisibilityAsync(
            db.MaintenanceTickets.AsNoTracking(),
            actor,
            cancellationToken);

        var status = query.Status?.Trim();
        if (!string.IsNullOrWhiteSpace(status))
            visibleTickets = visibleTickets.Where(ticket => ticket.Status == status);

        var priority = query.Priority?.Trim();
        if (!string.IsNullOrWhiteSpace(priority))
            visibleTickets = visibleTickets.Where(ticket => ticket.Priority == priority);

        if (query.EquipmentId.HasValue)
            visibleTickets = visibleTickets.Where(ticket => ticket.EquipmentId == query.EquipmentId.Value);

        // [NGHIỆP VỤ 1 - TÌM KIẾM ĐA TIÊU CHÍ DƯỚI SQL SERVER (Keyword)]:
        // Trong vận hành thực tế, nhân viên và kỹ thuật viên không nhớ mã TicketCode dài mà thường tìm theo:
        // - Tên hoặc Mã thiết bị ("Máy in Canon", "PRN-ACC-001") để tra cứu tiến độ hoặc lịch sử bệnh án thiết bị.
        // - Tên người yêu cầu, hoặc từ khóa trong tiêu đề/mô tả sự cố.
        // Lọc trực tiếp bằng câu lệnh SQL (EXISTS / LIKE) trước khi phân trang giúp tìm thấy chính xác
        // cả những phiếu cũ nằm ở Trang 2, Trang 3... thay vì chỉ lọc trên 10 dòng đầu ở trình duyệt.
        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            visibleTickets = visibleTickets.Where(ticket =>
                ticket.TicketCode.Contains(keyword) ||
                ticket.Title.Contains(keyword) ||
                ticket.Description.Contains(keyword) ||
                db.Equipment.Any(eq =>
                    eq.Id == ticket.EquipmentId &&
                    (eq.Code.Contains(keyword) || eq.Name.Contains(keyword))) ||
                db.Users.Any(u =>
                    u.Id == ticket.CreatedByUserId &&
                    u.FullName.Contains(keyword)));
        }

        // [NGHIỆP VỤ 2 - LỌC THEO TRẠNG THÁI CAM KẾT SLA DƯỚI SQL SERVER (SlaStatus)]:
        // Các phiếu bị Quá hạn (Breached) hoặc Sắp hết hạn (NearBreach) thường là các phiếu được tạo từ trước đó
        // nhiều giờ/nhiều ngày nên bị đẩy xuống các trang sau (Trang 2, Trang 3...) khi sắp xếp theo CreatedAt giảm dần.
        // Lọc trực tiếp dưới SQL Server theo thời gian thực (nowUtc so với DueAt) đảm bảo người Quản lý
        // kéo được toàn bộ các phiếu đang "cháy" SLA ở mọi trang lên ngay Trang 1 để điều phối khẩn cấp.
        var slaStatus = query.SlaStatus?.Trim();
        if (!string.IsNullOrWhiteSpace(slaStatus) && !slaStatus.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var nowUtc = DateTime.UtcNow;
            var nearBreachThresholdUtc = nowUtc.AddHours(1);

            visibleTickets = slaStatus switch
            {
                // Tạm dừng SLA: Phiếu đang chờ đối tác bên ngoài sửa chữa (WaitingForVendor)
                SlaPolicy.Paused => visibleTickets.Where(ticket =>
                    ticket.Status == TicketStatuses.WaitingForVendor ||
                    ticket.SlaStatus == SlaPolicy.Paused),

                // Đạt SLA: Phiếu đã hoàn thành/đóng đúng hạn cam kết
                SlaPolicy.MetSLA => visibleTickets.Where(ticket =>
                    (ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Closed) &&
                    (ticket.SlaStatus == SlaPolicy.MetSLA ||
                     (ticket.SlaStatus != SlaPolicy.MissedSLA &&
                      ticket.SlaStatus != SlaPolicy.Breached &&
                      (!ticket.DueAt.HasValue || !ticket.ResolvedAt.HasValue || ticket.ResolvedAt.Value <= ticket.DueAt.Value)))),

                // Quá hạn SLA (Breached / MissedSLA): Gồm các phiếu đang mở đã vượt quá DueAt
                // HOẶC các phiếu đã hoàn tất nhưng trễ hạn (MissedSLA).
                SlaPolicy.Breached => visibleTickets.Where(ticket =>
                    ((ticket.Status == TicketStatuses.Pending ||
                      ticket.Status == TicketStatuses.Assigned ||
                      ticket.Status == TicketStatuses.InProgress) &&
                     ticket.DueAt.HasValue &&
                     ticket.DueAt.Value < nowUtc) ||
                    ((ticket.Status == TicketStatuses.Resolved || ticket.Status == TicketStatuses.Closed) &&
                     (ticket.SlaStatus == SlaPolicy.MissedSLA ||
                      ticket.SlaStatus == SlaPolicy.Breached ||
                      (ticket.DueAt.HasValue && ticket.ResolvedAt.HasValue && ticket.ResolvedAt.Value > ticket.DueAt.Value)))),

                // Sắp hết hạn SLA (NearBreach): Phiếu đang xử lý và thời gian còn lại từ hiện tại đến DueAt <= 1 giờ
                SlaPolicy.NearBreach => visibleTickets.Where(ticket =>
                    (ticket.Status == TicketStatuses.Pending ||
                     ticket.Status == TicketStatuses.Assigned ||
                     ticket.Status == TicketStatuses.InProgress) &&
                    ticket.DueAt.HasValue &&
                    ticket.DueAt.Value >= nowUtc &&
                    ticket.DueAt.Value <= nearBreachThresholdUtc),

                // Trong hạn SLA (InSLA): Phiếu đang mở và còn hơn 1 giờ nữa mới đến hạn DueAt
                SlaPolicy.InSLA => visibleTickets.Where(ticket =>
                    (ticket.Status == TicketStatuses.Pending ||
                     ticket.Status == TicketStatuses.Assigned ||
                     ticket.Status == TicketStatuses.InProgress) &&
                    (!ticket.DueAt.HasValue || ticket.DueAt.Value > nearBreachThresholdUtc)),

                _ => visibleTickets
            };
        }

        var totalItems = await visibleTickets.CountAsync(cancellationToken);
        var responses = await visibleTickets
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ThenBy(ticket => ticket.Id)
            .ApplyPaging(query)
            .Select(TicketMappingExtensions.ResponseProjection)
            .ToListAsync(cancellationToken);

        await TicketMappingExtensions.PopulateAllExternalDetailsAsync(
            responses,
            identityDirectory,
            equipmentReader,
            vendorDirectory,
            cancellationToken);

        return responses.ToPagedResponse(query, totalItems);
    }
}

