namespace InternalMaintenance.Api.Models;

public class TicketStatusHistory
{
    public int Id {get;set;}
    public int MaintenanceTicketId {get;set;}
    public MaintenanceTicket? MaintenanceTicket {get;set;}

    public string OldStatus {get;set;} = string.Empty;
    public string NewStatus {get;set;} = string.Empty;
    public int ChangedByUserId {get;set;}

    public User? ChangedByUser {get;set;}
    public DateTime ChangedAt {get;set;} = DateTime.UtcNow;

    public string? Note {get;set;}
}