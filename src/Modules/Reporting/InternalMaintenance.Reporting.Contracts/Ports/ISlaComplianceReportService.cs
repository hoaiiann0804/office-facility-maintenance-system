namespace InternalMaintenance.Reporting.Contracts;

public interface ISlaComplianceReportService
{
    Task<SlaComplianceReportResponse> GetAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);
}
