namespace InternalMaintenance.Api.DTOs.TicketComment;
using System.ComponentModel.DataAnnotations;

public class CreateTicketCommentRequest
{
    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
}