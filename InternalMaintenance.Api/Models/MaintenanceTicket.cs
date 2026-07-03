namespace InternalMaintenance.Api.Models;

//MaintenanceTiket là phiếu báo lỗi/ yêu cầu bảo trì
//Khi một thiết bị hư, Staff sẽ tạo ticket. Sau đó Admin/Manager phân công 
//Techincian xử lý. Techician cập nhật trạng thái cho tới khi ticket được đóng 
public class MaintenanceTicket
{
    public int Id {get;set;}
    public string TicketCode {get;set;} = string.Empty;
    public string Title {get;set;} = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int EquipmentId {get;set;}

    //Thông tin chi tiết của thiết bị; chỉ có dữ liệu khi query có Include
    public Equipment? Equipment{get;set;}
    //Ai là người báo lỗi/ Tạo ticket  
    public int CreatedByUserId {get;set;}
    // Thông tin chi tiết ngươi tạo; chỉ có dữ liệu khi query có Include
    public User? CreatedByUser  {get;set;}
    //Tại sao là int? : 
    // Vì lúc ticket mới được tạo, chưa có ai giao xử lý.
    //Ban đầu
    //AssignedTechnicianId = null
    //Status = Pending

    //Sau khi Admin assign:
    //AssignedTechnicianId=3
    //Status = Assigned
    public int? AssignedTechnicianId {get;set;}
    // Thông tin chi tiết techincian xử lý 
    public User? AssignedTechnician {get;set;}
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending";

    //Vì ticket mới tạo hoặc đang xử lý thì chưa ghi chú giải pháp nên dùng ? (có thể null)
    public string? ResolutionNote  {get;set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // Thời điểm technicain báo xử lý xong
    //Ban đầu là null ==> thì chuyển sang ResolveAt= now (thời điểm xử lý xong) 
    public DateTime? ResolvedAt { get; set; }
    
    // Lưu thời điểm ticket được xác nhận hoàn tất

    // Khác với ResolvedAt.

    // ResolvedAt = technician báo đã xử lý xong
    // ClosedAt = staff/admin xác nhận hoàn tất

    // Ví dụ:

    // 09:45 Technician chuyển ticket sang Resolved
    // 10:00 Staff kiểm tra máy in và đóng ticket

    // Khi đó:

    // ResolvedAt = 09:45
    // ClosedAt = 10:00

    //Điều này thực tế hơn vì không phải cứ technician báo xong là công việc đã hoàn tất. Người báo lỗi có thể cần kiểm tra lại.
    public DateTime? ClosedAt { get; set; }

    // Lịch sử thay đổi trạng thái (1 MaintenanceTicket có nhiều TicketStatusHistory)
    public ICollection<TicketStatusHistory> StatusHistories {get;set;} = new List<TicketStatusHistory>();
    public ICollection<TicketComment> Comments {get;set;} = new List<TicketComment>();
    }