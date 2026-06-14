namespace InternalMaintenance.Api.DTOs;
public class MaintenanceTicketResponse
{
    public int Id {get;set;}
    public string TicketCode {get;set;} = string.Empty;
    public string Title {get;set;} = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int EquipmentId {get;set;}

    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName {get;set;} = string.Empty;

    public int CreatedByUserId {get;set;}

    public string CreatedByUserName {get;set;} = string.Empty;

    //Techincian được phân công xử lý
    // Có thể null vì ticket mới tạo có thể chưa được assign
    public int? AssignedTechnicianId {get;set;}
    public string? AssignedTechnicianName {get;set;}

    // Priority : Low, Medium, High, Urgent
    public string Priority {get;set;} = string.Empty;
    
    //Status hiện tại : Pending, Assigned, Resolved, Closed
    public string Status {get;set;} = string.Empty;

    // Kết quả xử lý cuối cùng, thường có khi status = Resolved/Closed
    public string? ResolutionNote {get;set;}

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }
}

