namespace InternalMaintenance.Api.Modules.Tickets.Contracts;

using System.ComponentModel.DataAnnotations;

public class CreateTicketCommentRequest
{
    [Required(ErrorMessage = "Content must not be empty or whitespace only.")]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
}
