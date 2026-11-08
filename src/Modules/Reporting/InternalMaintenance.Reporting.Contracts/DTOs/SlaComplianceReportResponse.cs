namespace InternalMaintenance.Reporting.Contracts;

public class SlaPriorityBreakdown
{
    public string Priority { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int MetSlaCount { get; set; }
    public int MissedSlaCount { get; set; }
    public double ComplianceRate { get; set; }
}

public class MonthlySlaTrend
{
    public string Month { get; set; } = string.Empty;
    public int TotalResolved { get; set; }
    public int MetSlaCount { get; set; }
    public int MissedSlaCount { get; set; }
    public double ComplianceRate { get; set; }
}

public class SlaComplianceReportResponse
{
    public int TotalTickets { get; set; }
    public int InSlaActiveCount { get; set; }
    public int NearBreachCount { get; set; }
    public int BreachedActiveCount { get; set; }
    public int PausedCount { get; set; }
    public int MetSlaCount { get; set; }
    public int MissedSlaCount { get; set; }
    public double OverallComplianceRate { get; set; }
    public List<SlaPriorityBreakdown> ByPriority { get; set; } = new();
    public List<MonthlySlaTrend> MonthlyTrends { get; set; } = new();
}
