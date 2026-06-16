namespace InternalMaintenance.Api.DTOs;

public class TicketStatusHistoryResponse
{
    public int Id {get;set;}
    public int MaintenanceTicketId {get;set;}

    public string OldStatus {get;set;} = string.Empty;
    public string NewStatus {get;set;} = string.Empty;

     public int ChangedByUserId { get; set; }

    public string ChangedByUserName { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    public string? Note { get; set; }
}