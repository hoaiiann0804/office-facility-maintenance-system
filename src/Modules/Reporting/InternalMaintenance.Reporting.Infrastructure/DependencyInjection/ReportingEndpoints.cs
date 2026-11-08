using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.ExportReportsToExcel;
using InternalMaintenance.Reporting.Infrastructure.Features.GetMaintenanceCostReport;
using InternalMaintenance.Reporting.Infrastructure.Features.GetSlaComplianceReport;
using InternalMaintenance.Reporting.Infrastructure.Features.GetTechnicianPerformanceReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Reporting.Infrastructure.DependencyInjection;

/// <summary>
/// [MODULE ENDPOINT REGISTRATION - REPORTING]
/// Gom toàn bộ 4 Minimal API Endpoints của các Vertical Slices trong module Reporting
/// vào chung một Route Group `/api/reports` theo chuẩn FullStackHero.
/// </summary>
public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/reports")
            .WithTags("Reports")
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{UserRoles.Admin},{UserRoles.Manager}" });

        group.MapGetTechnicianPerformanceReportEndpoint();
        group.MapGetSlaComplianceReportEndpoint();
        group.MapGetMaintenanceCostReportEndpoint();
        group.MapExportReportsToExcelEndpoint();

        return app;
    }
}

