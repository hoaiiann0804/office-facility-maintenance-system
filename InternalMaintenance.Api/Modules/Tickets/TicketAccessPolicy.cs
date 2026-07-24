using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Services;

namespace InternalMaintenance.Api.Modules.Tickets;

public static class TicketAccessPolicy
{
    public static IQueryable<MaintenanceTicket> Apply(
        IQueryable<MaintenanceTicket> query,
        CurrentUserService currentUserService)
    {
        var role = currentUserService.Role;
        var userId = currentUserService.UserId;
        var departmentId = currentUserService.DepartmentId;

        if (role == UserRoles.Admin)
        {
            return query;
        }

        if (role == UserRoles.Manager)
        {
            return query.Where(ticket => ticket.Equipment!.DepartmentId == departmentId);
        }

        if (role == UserRoles.Staff)
        {
            return query.Where(ticket => ticket.CreatedByUserId == userId);
        }

        if (role == UserRoles.Technician)
        {
            return query.Where(ticket => ticket.AssignedTechnicianId == userId || ticket.CreatedByUserId == userId);
        }

        return query.Where(ticket => false);
    }

    public static bool CanAccess(
        MaintenanceTicket ticket,
        CurrentUserService currentUserService)
    {
        var role = currentUserService.Role;

        if (role == UserRoles.Admin)
        {
            return true;
        }

        if (role == UserRoles.Manager)
        {
            return ticket.Equipment?.DepartmentId == currentUserService.DepartmentId;
        }

        if (role == UserRoles.Staff)
        {
            return ticket.CreatedByUserId == currentUserService.UserId;
        }

        if (role == UserRoles.Technician)
        {
            return ticket.AssignedTechnicianId == currentUserService.UserId || 
                   ticket.CreatedByUserId == currentUserService.UserId;
        }

        return false;
    }
}
