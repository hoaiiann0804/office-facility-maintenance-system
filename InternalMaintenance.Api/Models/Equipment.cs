namespace InternalMaintenance.Api.Models;

// Bảng Equipment là bảng lưu danh sách thiết bị công ty. 
// Mỗi thiết bị thuộc một phòng ban, có trạng thái hoạt động, ngày mua,mô tả và danh sách bảo trì liên quan
public class Equipment
{
    public int Id {get;set;}
    public string Code {get;set;} = string.Empty;
    public string Name {get;set;} = string.Empty;

    public int DepartmentId {get;set;}
    public Department? Department{get;set;}

    // Phòng ban chịu trách nhiệm bảo trì thiết bị này (VD: Phòng IT, Phòng Hành Chính)
    public int? MaintenanceDepartmentId {get;set;}
    public Department? MaintenanceDepartment {get;set;}

    public string Status{get;set;} ="Active";
    public DateTime? PurchasedDate {get;set;}

    public string? Description {get;set;}
    public DateTime CreatedAt {get;set;} = DateTime.UtcNow;

    public DateTime? UpdatedAt {get;set;}
    //Danh sách ticket/sự cố liên quan đến thiết bị này. (1-n)

    public ICollection<MaintenanceTicket> MaintenanceTickets {get;set;} = new List<MaintenanceTicket>();
};