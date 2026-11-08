using InternalMaintenance.Maintenance.Domain;

namespace Maintenance.Domain.Tests;

public sealed class MaintenanceTicketCreationTests
{
    [Fact]
    public void Create_trims_required_text_fields()
    {
        var ticket = MaintenanceTicket.Create(
            " MT-NEW ",
            " Printer issue ",
            " Paper feed is jammed ",
            equipmentId: 4,
            createdByUserId: 8,
            priority: TicketPriorities.Medium,
            createdAt: DateTime.UtcNow);

        Assert.Equal("MT-NEW", ticket.TicketCode);
        Assert.Equal("Printer issue", ticket.Title);
        Assert.Equal("Paper feed is jammed", ticket.Description);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("   ", "Description")]
    [InlineData("Title", "")]
    [InlineData("Title", "   ")]
    public void Create_rejects_blank_title_or_description(string title, string description)
    {
        Assert.Throws<ArgumentException>(() => MaintenanceTicket.Create(
            "MT-NEW",
            title,
            description,
            equipmentId: 4,
            createdByUserId: 8,
            priority: TicketPriorities.Medium,
            createdAt: DateTime.UtcNow));
    }
}
