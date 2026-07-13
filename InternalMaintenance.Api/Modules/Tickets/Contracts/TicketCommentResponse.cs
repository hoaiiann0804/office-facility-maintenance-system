namespace InternalMaintenance.Api.Modules.Tickets.Contracts;
public class TicketCommentResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
