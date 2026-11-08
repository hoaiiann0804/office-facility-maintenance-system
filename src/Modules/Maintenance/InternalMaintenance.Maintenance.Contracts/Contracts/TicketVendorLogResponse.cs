namespace InternalMaintenance.Maintenance.Contracts;

public class TicketVendorLogResponse
{
    public int Id { get; set; }
    public int MaintenanceTicketId { get; set; }
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public DateTime DispatchedAt { get; set; }
    public DateTime EstimatedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public int PausedMinutes { get; set; }
    public decimal? RepairCost { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
