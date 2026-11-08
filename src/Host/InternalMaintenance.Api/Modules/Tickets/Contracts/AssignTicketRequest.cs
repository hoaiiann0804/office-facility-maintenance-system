using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.Modules.Tickets.Contracts;

// Request dung cho viec phan cong ticket.
// Ticket van co 1 technician chinh va co the co them 1 technician ho tro.
public class AssignTicketRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "AssignedTechnicianId must be greater than 0")]
    public int AssignedTechnicianId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SupportTechnicianId must be greater than 0")]
    public int? SupportTechnicianId { get; set; }

    // Ghi chu cua lan phan cong nay.
    [StringLength(500, ErrorMessage = "Note must not exceed 500 characters")]
    public string? Note { get; set; }
}
