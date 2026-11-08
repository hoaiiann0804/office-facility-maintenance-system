using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Reporting.Infrastructure.Features.GetMaintenanceCostReport;

/// <summary>
/// [MINIMAL API ENDPOINT - BÁO CÁO CHI PHÍ BẢO TRÌ THUÊ NGOÀI]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/reports/maintenance-cost?fromDate=...&amp;toDate=...&amp;departmentId=...&amp;technicianId=...
///     -> GetMaintenanceCostReportEndpoint.MapGetMaintenanceCostReportEndpoint(...)
///     -> ReportFilterValidator.Validate(...)
///     -> IMaintenanceCostReportService.GetAsync(...) (điều phối xuống GetMaintenanceCostReportHandler)
///     -> Trả về HTTP 200 OK kèm MaintenanceCostReportResponse.
/// </summary>
public static class GetMaintenanceCostReportEndpoint
{
    public static RouteHandlerBuilder MapGetMaintenanceCostReportEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("maintenance-cost", async (
            [AsParameters] ReportFilterQuery filter,
            IMaintenanceCostReportService service,
            CancellationToken cancellationToken) =>
        {
            var validation = ReportFilterValidator.Validate(filter);
            if (validation is not null)
                return Results.BadRequest(new { message = validation });

            var report = await service.GetAsync(filter, cancellationToken);
            return Results.Ok(report);
        })
        .WithName("GetMaintenanceCostReport")
        .Produces<MaintenanceCostReportResponse>(StatusCodes.Status200OK);
    }
}

