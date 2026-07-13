namespace InternalMaintenance.Api.Modules.Tickets.Contracts;
using System.ComponentModel.DataAnnotations;

public class CreateTicketCommentRequest
{
    [Required]
    [RegularExpression(@"\S", ErrorMessage = "Content must not be whitespace only.")]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
}
