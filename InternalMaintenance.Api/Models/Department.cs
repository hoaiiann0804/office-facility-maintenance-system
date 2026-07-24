namespace InternalMaintenance.Api.Models;

public class Department
{
    public int Id{get;set;}
    public string Name{get;set;} = string.Empty;
    public string?Description {get;set;}
    public bool IsMaintenanceTeam { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<User> Users {get;set;} = new List<User>();
    public ICollection <Equipment> Equipment {get;set;} = new List<Equipment>();
}






