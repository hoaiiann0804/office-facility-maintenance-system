using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Reporting.Infrastructure.Features.GetSlaComplianceReport;

/// <summary>
/// [MINIMAL API ENDPOINT - BÁO CÁO TUÂN THỦ SLA]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/reports/sla-compliance?fromDate=...&amp;toDate=...&amp;departmentId=...&amp;technicianId=...
///     -> GetSlaComplianceReportEndpoint.MapGetSlaComplianceReportEndpoint(...)
///     -> ReportFilterValidator.Validate(...)
///     -> ISlaComplianceReportService.GetAsync(...) (điều phối xuống GetSlaComplianceReportHandler)
///     -> Trả về HTTP 200 OK kèm SlaComplianceReportResponse.
/// </summary>
public static class GetSlaComplianceReportEndpoint
{
    public static RouteHandlerBuilder MapGetSlaComplianceReportEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("sla-compliance", async (
            [AsParameters] ReportFilterQuery filter,
            ISlaComplianceReportService service,
            CancellationToken cancellationToken) =>
        {
            var validation = ReportFilterValidator.Validate(filter);
            if (validation is not null)
                return Results.BadRequest(new { message = validation });

            var report = await service.GetAsync(filter, cancellationToken);
            return Results.Ok(report);
        })
        .WithName("GetSlaComplianceReport")
        .Produces<SlaComplianceReportResponse>(StatusCodes.Status200OK);
    }
}

