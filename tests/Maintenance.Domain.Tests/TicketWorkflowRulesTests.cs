using InternalMaintenance.Api.Modules.Tickets;
using InternalMaintenance.Maintenance.Domain;

namespace Maintenance.Domain.Tests;

public class TicketWorkflowRulesTests
{
    [Fact]
    public void Resolved_ticket_occupies_equipment_but_does_not_run_sla()
    {
        Assert.Contains(TicketStatuses.Resolved, TicketWorkflowRules.EquipmentOccupyingStatuses);
        Assert.DoesNotContain(TicketStatuses.Resolved, TicketWorkflowRules.SlaTrackedStatuses);
    }

    [Fact]
    public void Waiting_for_vendor_occupies_equipment_but_does_not_run_sla()
    {
        Assert.Contains(TicketStatuses.WaitingForVendor, TicketWorkflowRules.EquipmentOccupyingStatuses);
        Assert.DoesNotContain(TicketStatuses.WaitingForVendor, TicketWorkflowRules.SlaTrackedStatuses);
    }

    [Theory]
    [InlineData(TicketStatuses.Pending, true)]
    [InlineData(TicketStatuses.Assigned, true)]
    [InlineData(TicketStatuses.InProgress, true)]
    [InlineData(TicketStatuses.Resolved, false)]
    [InlineData(TicketStatuses.Closed, false)]
    [InlineData(TicketStatuses.Cancelled, false)]
    [InlineData(TicketStatuses.WaitingForVendor, false)]
    public void Sla_tracked_statuses_match_business_meaning(string status, bool expected)
    {
        Assert.Equal(expected, TicketWorkflowRules.SlaTrackedStatuses.Contains(status));
    }
}
