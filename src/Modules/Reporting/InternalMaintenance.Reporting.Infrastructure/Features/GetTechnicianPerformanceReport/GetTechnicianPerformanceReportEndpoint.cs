using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Reporting.Infrastructure.Features.GetTechnicianPerformanceReport;

/// <summary>
/// [MINIMAL API ENDPOINT - BÁO CÁO HIỆU SUẤT KỸ THUẬT VIÊN]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/reports/technician-performance?fromDate=...&amp;toDate=...&amp;departmentId=...&amp;technicianId=...
///     -> GetTechnicianPerformanceReportEndpoint.MapGetTechnicianPerformanceReportEndpoint(...)
///     -> ReportFilterValidator.Validate(...)
///     -> ITechnicianPerformanceReportService.GetAsync(...) (điều phối xuống GetTechnicianPerformanceReportHandler)
///     -> Trả về HTTP 200 OK kèm List&lt;TechnicianPerformanceReportResponse&gt;.
/// </summary>
public static class GetTechnicianPerformanceReportEndpoint
{
    public static RouteHandlerBuilder MapGetTechnicianPerformanceReportEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("technician-performance", async (
            [AsParameters] ReportFilterQuery filter,
            ITechnicianPerformanceReportService service,
            CancellationToken cancellationToken) =>
        {
            var validation = ReportFilterValidator.Validate(filter);
            if (validation is not null)
                return Results.BadRequest(new { message = validation });

            var result = await service.GetAsync(filter, cancellationToken);
            return Results.Ok(result.ToList());
        })
        .WithName("GetTechnicianPerformanceReport")
        .Produces<List<TechnicianPerformanceReportResponse>>(StatusCodes.Status200OK);
    }
}

