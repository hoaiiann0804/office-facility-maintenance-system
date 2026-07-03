namespace InternalMaintenance.Api.Models;
public class  TicketComment
{
    public int Id {get;set;}
    public int MaintenanceTicketId  {get;set;}
    public MaintenanceTicket? MaintenanceTicket {get;set;}
    public int UserId {get;set;}
    public User? User {get;set;}
    public string Content {get;set;} = string.Empty;
    public DateTime CreatedAt { get; set; }

}