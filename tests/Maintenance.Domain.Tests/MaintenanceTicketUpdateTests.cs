using InternalMaintenance.Maintenance.Domain;

namespace Maintenance.Domain.Tests;

public sealed class MaintenanceTicketUpdateTests
{
    [Fact]
    public void Update_details_trims_valid_text()
    {
        var ticket = NewTicket();

        ticket.UpdateDetails("  Printer repair  ", "  Replace the feed roller  ");

        Assert.Equal("Printer repair", ticket.Title);
        Assert.Equal("Replace the feed roller", ticket.Description);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("   ", "Description")]
    [InlineData("Title", "")]
    [InlineData("Title", "   ")]
    public void Update_details_rejects_blank_title_or_description(string title, string description)
    {
        var ticket = NewTicket();

        Assert.Throws<ArgumentException>(() => ticket.UpdateDetails(title, description));
        Assert.Equal("Original title", ticket.Title);
        Assert.Equal("Original description", ticket.Description);
    }

    [Fact]
    public void Finalized_ticket_cannot_be_edited()
    {
        var ticket = NewTicket();
        ticket.Assign(20, null);
        ticket.Start(DateTime.UtcNow);
        ticket.Resolve("Repaired", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            ticket.UpdateDetails("New title", "New description"));
        Assert.Equal("Original title", ticket.Title);
        Assert.Equal("Original description", ticket.Description);
    }

    private static MaintenanceTicket NewTicket() => MaintenanceTicket.Create(
        "MT-UPDATE-TEST",
        "Original title",
        "Original description",
        equipmentId: 10,
        createdByUserId: 30,
        priority: TicketPriorities.Medium,
        createdAt: DateTime.UtcNow);
}
