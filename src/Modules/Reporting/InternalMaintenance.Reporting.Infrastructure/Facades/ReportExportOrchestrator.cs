using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.ExportReportsToExcel;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Reporting.Infrastructure;

/// <summary>
/// [THIN FAÇADE - XUẤT BÁO CÁO TỔNG HỢP RA EXCEL]
/// Điều phối lời gọi từ `IReportExportOrchestrator` xuống `ExportReportsToExcelHandler`.
/// </summary>
public sealed class ReportExportOrchestrator : IReportExportOrchestrator
{
    private readonly ExportReportsToExcelHandler _handler;

    [ActivatorUtilitiesConstructor]
    public ReportExportOrchestrator(ExportReportsToExcelHandler handler)
    {
        _handler = handler;
    }

    public ReportExportOrchestrator(
        ITechnicianPerformanceReportService technicianReport,
        ISlaComplianceReportService slaReport,
        IMaintenanceCostReportService costReport,
        ReportExportService excelWriter)
        : this(new ExportReportsToExcelHandler(technicianReport, slaReport, costReport, excelWriter))
    {
    }

    public Task<byte[]> ExportExcelAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default) =>
        _handler.HandleAsync(filter, cancellationToken);
}
