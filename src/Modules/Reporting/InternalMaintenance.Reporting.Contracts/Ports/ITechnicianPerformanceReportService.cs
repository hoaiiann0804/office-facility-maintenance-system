namespace InternalMaintenance.Reporting.Contracts;

public interface ITechnicianPerformanceReportService
{
    Task<IReadOnlyCollection<TechnicianPerformanceReportResponse>> GetAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);
}
