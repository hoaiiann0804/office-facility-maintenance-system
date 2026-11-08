namespace InternalMaintenance.Reporting.Contracts;

public interface IMaintenanceCostReportService
{
    Task<MaintenanceCostReportResponse> GetAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);
}
