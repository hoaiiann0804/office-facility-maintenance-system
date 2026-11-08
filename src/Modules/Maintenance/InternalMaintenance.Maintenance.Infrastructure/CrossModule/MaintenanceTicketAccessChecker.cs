using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>
/// Access policy implementation thuộc module sở hữu ticket. Các module gọi contract công khai;
/// quy tắc Manager cần dữ liệu phòng ban thì hỏi Assets/Identity qua contract tương ứng.
/// </summary>
public sealed class MaintenanceTicketAccessChecker(
    IEquipmentTicketContextReader equipmentReader,
    IIdentityUserDirectory identityDirectory) : IMaintenanceTicketAccessChecker
{
    public async Task<bool> CanAccessAsync(
        MaintenanceTicketAccessContext ticket,
        MaintenanceTicketActor actor,
        CancellationToken cancellationToken = default)
    {
        if (actor.Role == UserRoles.Admin)
            return true;
        if (actor.Role == UserRoles.Staff)
            return ticket.CreatedByUserId == actor.UserId;
        if (actor.Role == UserRoles.Technician)
            return ticket.CreatedByUserId == actor.UserId ||
                   ticket.AssignedTechnicianId == actor.UserId ||
                   ticket.SupportTechnicianId == actor.UserId;
        if (actor.Role != UserRoles.Manager || !actor.DepartmentId.HasValue)
            return false;

        if (ticket.CreatedByUserId == actor.UserId)
            return true;

        var equipment = await equipmentReader.GetAsync(ticket.EquipmentId, cancellationToken);
        if (equipment is not null &&
            (equipment.DepartmentId == actor.DepartmentId ||
             equipment.MaintenanceDepartmentId == actor.DepartmentId))
            return true;

        var requesterDepartmentId = await identityDirectory.FindDepartmentIdAsync(
            ticket.CreatedByUserId,
            cancellationToken);
        return requesterDepartmentId == actor.DepartmentId;
    }
}
