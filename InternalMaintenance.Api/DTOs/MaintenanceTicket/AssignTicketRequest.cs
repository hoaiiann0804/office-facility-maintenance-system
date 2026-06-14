using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.DTOs;

// Request này chỉ phục vụ phân công ticket cho technician.
//KHông đại diện cho một bảng riêng cho db 
// Khi client gọi : 

//PATCH /api/tickets{id}/assign

//Backend sẽ dùng dữ liệu này để: 
//1. Cập nhật Ticket hiện tại : 
//MaintenanceTicket.AssignedTechnicianId  = request.AssignedTechnicianId
//MaintenanceTicket.Status = "Assigned"

//2. Nếu có note, lưu ghi chú này vào lịch xử để xử lý 

//Vì vậy
//AssignedTechinicianId được lưu vào bảng MaintenanceTicket
// Nên note ko cần thêm vào MaintenanceTickets,
// Vì note là ghi chú riêng "Làm phân công xử lý ticket"
// không phải thông tin chính cố định của ticket
public class AssignTicketRequest {
    [Range(1,int.MaxValue, ErrorMessage = "EquipmentId must be greater than 0")]

    public int AssignedTechnicianId {get;set;}

    //Ghi chú thêm cho lần phân công ticket
    //Ví dụ xử lý trong hôm nay vì phòng kế toán đang cần in hóa đơn 
    //Field này không nên lưu trực tiếp vào MaintenanceTickets,
    //Vì một ticket có nhiều hành động khác nhau
    //- Assigned
    //- InProgress
    //- Resolved
    //- Closed

    // Mỗi hành động có thể một ghi chú riêng 
    // Nếu lưu Note vào MaintenanceTickets thì các ghi chú cũ dễ bị đè.

    //Cách đúng hơn:
    // Lưu Note này vào TicketStatusHistory.Note để giữ lại timeline xử lý.
    [StringLength(500, ErrorMessage = "Note must not exceed 500 charcters")]
    public string? Note { get; set; }
}