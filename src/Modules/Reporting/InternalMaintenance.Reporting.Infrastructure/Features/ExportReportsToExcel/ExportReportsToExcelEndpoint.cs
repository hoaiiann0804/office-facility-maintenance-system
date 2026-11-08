using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Reporting.Infrastructure.Features.ExportReportsToExcel;

/// <summary>
/// [MINIMAL API ENDPOINT - XUẤT BÁO CÁO TỔNG HỢP RA FILE EXCEL]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/reports/export/excel?fromDate=...&amp;toDate=...&amp;departmentId=...&amp;technicianId=...
///     -> ExportReportsToExcelEndpoint.MapExportReportsToExcelEndpoint(...)
///     -> ReportFilterValidator.Validate(...)
///     -> IReportExportOrchestrator.ExportExcelAsync(...) (điều phối xuống ExportReportsToExcelHandler)
///     -> Trả về File Excel (.xlsx) với Content-Type chuẩn OpenXML.
/// </summary>
public static class ExportReportsToExcelEndpoint
{
    public static RouteHandlerBuilder MapExportReportsToExcelEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("export/excel", async (
            [AsParameters] ReportFilterQuery filter,
            IReportExportOrchestrator exportOrchestrator,
            CancellationToken cancellationToken) =>
        {
            var validation = ReportFilterValidator.Validate(filter);
            if (validation is not null)
                return Results.BadRequest(new { message = validation });

            var fileBytes = await exportOrchestrator.ExportExcelAsync(filter, cancellationToken);
            var fileName = $"BaoCao_BaoTri_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return Results.File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        })
        .WithName("ExportReportsToExcel");
    }
}

