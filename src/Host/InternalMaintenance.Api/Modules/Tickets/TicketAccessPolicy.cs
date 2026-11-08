using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Api.Modules.Tickets.Contracts;
using InternalMaintenance.Api.Services;

namespace InternalMaintenance.Api.Modules.Tickets;

public static class TicketAccessPolicy
{
    /// <summary>
    /// Lọc list query trước khi đếm/phân trang để không đưa ticket ngoài phạm vi quyền về process.
    /// Danh sách ID phòng ban được hỏi qua contract của Assets và Identity; predicate ticket được SQL thực thi.
    /// </summary>
    public static async Task<IQueryable<MaintenanceTicket>> ApplyAsync(
        IQueryable<MaintenanceTicket> query,
        CurrentUserService currentUserService,
        IIdentityUserDirectory identityUserDirectory,
        IEquipmentTicketContextReader equipmentReader,
        CancellationToken cancellationToken = default)
    {
        var role = currentUserService.Role;
        var userId = currentUserService.UserId;
        var departmentId = currentUserService.DepartmentId;

        if (role == UserRoles.Admin)
            return query;

        if (role == UserRoles.Manager)
        {
            // Hai module cung cấp ID theo contract; join/lọc trên ticket vẫn được SQL Server thực hiện.
            var requesterIdsInDepartment = await identityUserDirectory.FindUserIdsByDepartmentAsync(
                departmentId,
                cancellationToken);
            var equipmentIdsInDepartment = await equipmentReader.FindIdsInDepartmentAsync(
                departmentId,
                cancellationToken);
            return query.Where(ticket =>
                equipmentIdsInDepartment.Contains(ticket.EquipmentId) ||
                ticket.CreatedByUserId == userId ||
                requesterIdsInDepartment.Contains(ticket.CreatedByUserId));
        }

        if (role == UserRoles.Staff)
            return query.Where(ticket => ticket.CreatedByUserId == userId);

        if (role == UserRoles.Technician)
        {
            return query.Where(ticket =>
                ticket.AssignedTechnicianId == userId ||
                ticket.SupportTechnicianId == userId ||
                ticket.CreatedByUserId == userId);
        }

        return query.Where(_ => false);
    }

    /// <summary>
    /// Kiểm tra quyền trên một ticket đã tải. Dữ liệu phòng ban được hỏi qua contract của
    /// module sở hữu; API dùng ticket subject thay vì IQueryable entity, vì vậy ranh giới HTTP
    /// không cần truy cập Domain/EF của Maintenance để quyết định quyền.
    /// </summary>
    public static async Task<bool> CanAccessAsync(
        TicketAccessSubject ticket,
        CurrentUserService currentUserService,
        IEquipmentTicketContextReader equipmentReader,
        IIdentityUserDirectory identityUserDirectory,
        CancellationToken cancellationToken = default)
    {
        var role = currentUserService.Role;
        var userId = currentUserService.UserId;

        if (role == UserRoles.Admin)
            return true;

        if (role == UserRoles.Staff)
            return ticket.CreatedByUserId == userId;

        if (role == UserRoles.Technician)
        {
            return ticket.AssignedTechnicianId == userId ||
                   ticket.SupportTechnicianId == userId ||
                   ticket.CreatedByUserId == userId;
        }

        if (role != UserRoles.Manager)
            return false;

        var departmentId = currentUserService.DepartmentId;
        if (!departmentId.HasValue)
            return false;

        // Giữ đúng quyền Manager tự tạo ticket, không cần gọi sang module khác.
        if (ticket.CreatedByUserId == userId)
            return true;

        var equipment = await equipmentReader.GetAsync(ticket.EquipmentId, cancellationToken);
        if (equipment is not null &&
            (equipment.DepartmentId == departmentId || equipment.MaintenanceDepartmentId == departmentId))
        {
            return true;
        }

        // Chỉ đọc Identity nếu ticket không thuộc phòng sở hữu/queue bảo trì của Manager.
        // Cách hỏi có điều kiện tránh truy vấn User cho những trường hợp đã đủ căn cứ từ Assets.
        var requesterDepartmentId = await identityUserDirectory.FindDepartmentIdAsync(
            ticket.CreatedByUserId,
            cancellationToken);
        return requesterDepartmentId == departmentId;
    }
}
