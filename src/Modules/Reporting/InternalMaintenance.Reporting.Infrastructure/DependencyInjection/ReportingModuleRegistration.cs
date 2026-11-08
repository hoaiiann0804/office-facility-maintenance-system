using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.ExportReportsToExcel;
using InternalMaintenance.Reporting.Infrastructure.Features.GetMaintenanceCostReport;
using InternalMaintenance.Reporting.Infrastructure.Features.GetSlaComplianceReport;
using InternalMaintenance.Reporting.Infrastructure.Features.GetTechnicianPerformanceReport;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Reporting.Infrastructure.DependencyInjection;

/// <summary>
/// [ĐĂNG KÝ DI CHO VERTICAL SLICES CỦA MODULE REPORTING]
/// </summary>
public static class ReportingModuleRegistration
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<ReportExportService>();

        services.AddScoped<GetSlaComplianceReportHandler>();
        services.AddScoped<GetTechnicianPerformanceReportHandler>();
        services.AddScoped<GetMaintenanceCostReportHandler>();
        services.AddScoped<ExportReportsToExcelHandler>();

        services.AddScoped<ISlaComplianceReportService>(sp =>
            ActivatorUtilities.CreateInstance<SlaComplianceReportService>(sp));
        services.AddScoped<ITechnicianPerformanceReportService>(sp =>
            ActivatorUtilities.CreateInstance<TechnicianPerformanceReportService>(sp));
        services.AddScoped<IMaintenanceCostReportService>(sp =>
            ActivatorUtilities.CreateInstance<MaintenanceCostReportService>(sp));
        services.AddScoped<IReportExportOrchestrator>(sp =>
            ActivatorUtilities.CreateInstance<ReportExportOrchestrator>(sp));
        return services;
    }
}
