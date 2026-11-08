namespace InternalMaintenance.Api.Modules.Dashboard.Contracts;

public class DashboardSummaryResponse
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }

    public int TotalEquipment { get; set; }
    public int ActiveEquipment { get; set; }
    public int UnderMaintenanceEquipment { get; set; }

    public int TotalTechnicians { get; set; }
    public int TotalDepartments { get; set; }
}