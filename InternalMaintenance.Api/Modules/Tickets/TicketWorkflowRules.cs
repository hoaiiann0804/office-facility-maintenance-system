using InternalMaintenance.Api.Constants;

namespace InternalMaintenance.Api.Modules.Tickets;

public static class TicketWorkflowRules
{
    public static bool IsAllowedPriority(string priority)
    {
        var allowedPriorities = new[]
        {
            TicketPriorities.Low,
            TicketPriorities.Medium,
            TicketPriorities.High,
            TicketPriorities.Critical
        };

        return allowedPriorities.Contains(priority);
    }

    public static bool IsOpenTicketStatus(string status)
    {
        var openStatuses = new[]
        {
            TicketStatuses.Pending,
            TicketStatuses.Assigned,
            TicketStatuses.InProgress,
            TicketStatuses.Resolved
        };

        return openStatuses.Contains(status);
    }

    public static bool IsAssignableStatus(string status)
    {
        var assignableStatuses = new[]
        {
            TicketStatuses.Pending,
            TicketStatuses.Assigned
        };

        return assignableStatuses.Contains(status);
    }

    public static bool CanTransition(string currentStatus, string newStatus)
    {
        return
            (currentStatus == TicketStatuses.Pending && newStatus == TicketStatuses.Cancelled) ||
            (currentStatus == TicketStatuses.Assigned && newStatus == TicketStatuses.Cancelled) ||
            (currentStatus == TicketStatuses.Assigned && newStatus == TicketStatuses.InProgress) ||
            (currentStatus == TicketStatuses.InProgress && newStatus == TicketStatuses.Resolved) ||
            (currentStatus == TicketStatuses.Resolved && newStatus == TicketStatuses.Closed);
    }
}
