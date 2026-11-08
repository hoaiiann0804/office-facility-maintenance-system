namespace InternalMaintenance.Reporting.Contracts;

public class TechnicianPerformanceReportResponse
{
    public int TechnicianId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalAssigned { get; set; }
    public int TotalResolved { get; set; }
    public int TotalClosed { get; set; }
    public int InProgressCount { get; set; }
    public int WaitingForVendorCount { get; set; }
    public int MetSlaCount { get; set; }
    public int MissedSlaCount { get; set; }
    public double SlaComplianceRate { get; set; }
    public double AvgResolutionHours { get; set; }
}
