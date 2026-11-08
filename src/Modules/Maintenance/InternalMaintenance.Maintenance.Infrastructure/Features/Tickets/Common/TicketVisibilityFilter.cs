using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.Common;

/// <summary>
/// [SHARED CONCERN TRONG NỘI BỘ MODULE MAINTENANCE - PHÂN QUYỀN TẦM NHÌN TICKET (RBAC VISIBILITY)]
///
/// Giải quyết vấn đề khi tái cấu trúc:
/// - Cả 4 Vertical Slices đọc dữ liệu Ticket (`GetTicketsPage`, `GetTicketById`, `GetTicketComments`, `GetTicketHistory`)
///   đều phải áp dụng cùng một quy tắc phân quyền tầm nhìn (Visibility Scope) TRƯỚC khi đếm (`CountAsync`) hoặc đọc chi tiết:
///   + `Admin`: Xem toàn bộ phiếu bảo trì.
///   + `Manager`: Xem phiếu của thiết bị thuộc phòng mình quản lý, hoặc phiếu do nhân sự phòng mình tạo.
///   + `Technician`: Xem phiếu được giao chính (`AssignedTechnicianId`), giao phụ (`SupportTechnicianId`) hoặc tự tạo.
///   + `Staff`: Chỉ xem phiếu do chính mình tạo (`CreatedByUserId`).
/// - Tách vào `TicketVisibilityFilter` giúp cả 4 Slices dùng chung một bộ lọc SQL chuẩn xác, không trùng lặp code.
/// </summary>
public sealed class TicketVisibilityFilter(
    IIdentityUserDirectory identityDirectory,
    IEquipmentTicketContextReader equipmentReader)
{
    public async Task<IQueryable<MaintenanceTicket>> ApplyVisibilityAsync(
        IQueryable<MaintenanceTicket> query,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role == UserRoles.Admin)
            return query;

        if (actor.Role == UserRoles.Manager)
        {
            var requesterIds = await identityDirectory.FindUserIdsByDepartmentAsync(
                actor.DepartmentId, cancellationToken);
            var equipmentIds = await equipmentReader.FindIdsInDepartmentAsync(
                actor.DepartmentId, cancellationToken);

            return query.Where(ticket => equipmentIds.Contains(ticket.EquipmentId) ||
                                         ticket.CreatedByUserId == actor.UserId ||
                                         requesterIds.Contains(ticket.CreatedByUserId));
        }

        if (actor.Role == UserRoles.Staff)
            return query.Where(ticket => ticket.CreatedByUserId == actor.UserId);

        if (actor.Role == UserRoles.Technician)
            return query.Where(ticket => ticket.AssignedTechnicianId == actor.UserId ||
                                         ticket.SupportTechnicianId == actor.UserId ||
                                         ticket.CreatedByUserId == actor.UserId);

        return query.Where(_ => false);
    }
}

