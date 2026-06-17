using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.DTOs.MaintenanceTicket;

public class UpdateTicketStatusRequest
{
    // dùng để thay đổi trạng thái 
    // Pending -> Assigned -> InProgres -> Rosolved -> Close

    // Trạng thái mới mà client muốn đổi sang.
    // KHông đặt mặc định là "Pending", vì đây là request đổi trạng thái
    // Client phải gửi rõ muốn đổi sang Assigned, InProgress, Resolved hoặc Closed

    //Nếu thiếu Status, API phải thông báo lỗi validation thay vì tự hiểu "Pending"
    [Required(ErrorMessage = "Status is required")]
    [StringLength(30, ErrorMessage = "Status must not exceed 30 characters")]
    public string Status { get; set; } = string.Empty;

    // Kết quả xử lý cuối cùng, thường chỉ cần khi status = Resolved.
    public string? ResolutionNote {get;set;}

    // Ghi chú lần đổi trạng thái này, lưu vào TicketStatusHistory.Note.
    public string? Note {get;set;}
}