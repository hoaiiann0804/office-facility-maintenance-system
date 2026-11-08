using InternalMaintenance.Reporting.Contracts;

namespace InternalMaintenance.Reporting.Infrastructure.Features.ExportReportsToExcel;

/// <summary>
/// [VERTICAL SLICE: EXPORT REPORTS TO EXCEL - XUẤT TỔNG HỢP 3 BÁO CÁO RA FILE EXCEL (.XLSX)]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/reports/export (ReportsController)
///           -> IReportExportOrchestrator.ExportExcelAsync(filter)
///           -> ExportReportsToExcelHandler.HandleAsync(filter)
///           -> Bước 1: Gọi tuần tự 3 dịch vụ báo cáo (`ITechnicianPerformanceReportService`,
///              `ISlaComplianceReportService`, `IMaintenanceCostReportService`) để tránh xung đột DbContext Scoped.
///           -> Bước 2: Chuyển dữ liệu vào `ReportExportService` (ClosedXML) để kết xuất workbook Excel nhiều sheet.
///           -> Bước 3: Trả về mảng `byte[]` của file `.xlsx`.
/// </summary>
public sealed class ExportReportsToExcelHandler(
    ITechnicianPerformanceReportService technicianReport,
    ISlaComplianceReportService slaReport,
    IMaintenanceCostReportService costReport,
    ReportExportService excelWriter)
{
    public async Task<byte[]> HandleAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var technicians = await technicianReport.GetAsync(filter, cancellationToken);
        var sla = await slaReport.GetAsync(filter, cancellationToken);
        var costs = await costReport.GetAsync(filter, cancellationToken);

        return excelWriter.ExportAllReportsToExcel(
            technicians.ToList(),
            sla,
            costs,
            filter);
    }
}

