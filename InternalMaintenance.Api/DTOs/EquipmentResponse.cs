
using InternalMaintenance.Api.Models;

namespace InternalMaintenance.Api.DTOs.Equipment;
public class EquipmentResponse
{
    public int Id {get;set;}
    public string Code {get;set;} = string.Empty;
    public string Name {get;set;} = string.Empty;

    public int DepartmentId {get;set;}
    public string DepartmentName{get;set;} = string.Empty;
    public string Status{get;set;} ="Active";
    public DateTime? PurchasedDate {get;set;}

    public string? Description {get;set;}
    public DateTime CreatedAt {get;set;} = DateTime.UtcNow;
    public DateTime? UpdatedAt {get;set;}
}