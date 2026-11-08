namespace InternalMaintenance.Reporting.Contracts;

public interface IReportExportOrchestrator
{
    Task<byte[]> ExportExcelAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);
}
