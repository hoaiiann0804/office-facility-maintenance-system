using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.Modules.Tickets.Contracts;

public class ChangeTicketStatusRequest
{
    // Dung de thay doi trang thai ticket trong workflow.
    // Thu tu tham chieu: Pending -> Assigned -> InProgress -> Resolved -> Closed.

    // Trang thai moi ma client muon doi sang.
    // Khong dat mac dinh la Pending vi day la request doi trang thai.
    // Client phai gui ro trang thai muon chuyen sang.

    // Neu thieu Status, API phai tra loi validation thay vi tu hieu ngam.
    [Required(ErrorMessage = "Status is required")]
    [StringLength(30, ErrorMessage = "Status must not exceed 30 characters")]
    public string Status { get; set; } = string.Empty;

    // Ket qua xu ly cuoi cung, thuong chi can khi status = Resolved.
    public string? ResolutionNote { get; set; }

    // Ly do huy ticket, dung khi status = Cancelled.
    public string? CancellationReason { get; set; }

    // Ghi chu cho lan doi trang thai nay, se luu vao TicketStatusHistory.Note.
    public string? Note { get; set; }

    // Vendor Management (khi status = WaitingForVendor)
    public int? VendorId { get; set; }
    public DateTime? VendorEstimatedReturnDate { get; set; }
    public string? VendorNote { get; set; }
}
