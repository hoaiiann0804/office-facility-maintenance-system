namespace InternalMaintenance.Reporting.Contracts;

// Query/response DTOs thuộc contract của Reporting nên host có thể bind HTTP mà không kéo implementation vào API.
public class ReportFilterQuery
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? DepartmentId { get; set; }
    public int? TechnicianId { get; set; }
}
