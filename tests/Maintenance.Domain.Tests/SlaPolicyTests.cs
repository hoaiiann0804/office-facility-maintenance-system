using InternalMaintenance.Maintenance.Domain;

namespace Maintenance.Domain.Tests;

public class SlaPolicyTests
{
    [Fact]
    public void Priority_due_date_includes_pause_minutes()
    {
        var createdAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var dueAt = SlaPolicy.CalculateDueAt(createdAt, TicketPriorities.Medium).AddMinutes(360);

        Assert.Equal(new DateTime(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc), dueAt);
    }

    [Theory]
    [InlineData(-120, SlaPolicy.InSLA)]
    [InlineData(-30, SlaPolicy.NearBreach)]
    [InlineData(1, SlaPolicy.Breached)]
    public void Active_status_is_based_on_current_due_time(int minutesAfterDeadline, string expected)
    {
        var now = new DateTime(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc);
        var dueAt = now.AddMinutes(-minutesAfterDeadline);

        Assert.Equal(expected, SlaPolicy.GetActiveStatus(dueAt, now));
    }
}
