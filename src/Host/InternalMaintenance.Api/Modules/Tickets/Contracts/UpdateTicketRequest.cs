namespace InternalMaintenance.Api.Modules.Tickets.Contracts;

public class UpdateTicketRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;
}
