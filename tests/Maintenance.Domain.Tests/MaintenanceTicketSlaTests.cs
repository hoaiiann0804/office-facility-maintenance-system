using InternalMaintenance.Maintenance.Domain;

namespace Maintenance.Domain.Tests;

public class MaintenanceTicketSlaTests
{
    [Fact]
    public void Resolved_on_time_ticket_never_receives_later_breach_notification()
    {
        var createdAt = Utc(2026, 5, 1, 8);
        var ticket = CreateInProgress(createdAt);
        var dueAt = ticket.DueAt!.Value;
        ticket.Resolve("Repaired", dueAt.AddHours(-1));

        var notifications = ticket.GetDueSlaNotifications(dueAt.AddHours(1));

        Assert.Equal(TicketStatuses.Resolved, ticket.Status);
        Assert.Equal(SlaPolicy.MetSLA, ticket.SlaStatus);
        Assert.Empty(notifications);
    }

    [Fact]
    public void Overdue_active_ticket_emits_breach_and_escalation_notifications()
    {
        var ticket = MaintenanceTicket.Create("MT-TEST", "Printer issue", "Description",
            1, 1, TicketPriorities.Medium, Utc(2026, 5, 1, 8));

        var notifications = ticket.GetDueSlaNotifications(ticket.DueAt!.Value.AddHours(3));

        Assert.Equal(
            new[] { SlaNotificationType.Breached, SlaNotificationType.Escalated },
            notifications);
    }

    [Fact]
    public void Returning_from_vendor_preserves_pause_when_priority_changes()
    {
        var createdAt = Utc(2026, 5, 1, 8);
        var ticket = CreateInProgress(createdAt);
        ticket.SendToVendor(Utc(2026, 5, 1, 12));
        ticket.ReturnFromVendor(Utc(2026, 5, 1, 18), 360);

        ticket.ChangePriority(TicketPriorities.High, Utc(2026, 5, 1, 18));

        Assert.Equal(Utc(2026, 5, 1, 22), ticket.DueAt);
        Assert.Equal(360, ticket.TotalSlaPausedMinutes);
    }

    private static MaintenanceTicket CreateInProgress(DateTime createdAt)
    {
        var ticket = MaintenanceTicket.Create("MT-TEST", "Printer issue", "Description",
            1, 1, TicketPriorities.Medium, createdAt);
        ticket.Assign(2, null);
        ticket.Start(createdAt.AddMinutes(1));
        return ticket;
    }

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
