using InternalMaintenance.Reporting.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Api.Modules.Reports;

/// <summary>
/// Đăng ký các dịch vụ đọc và xuất báo cáo tại chính module Reports.
/// Host chỉ bật module, không cần liệt kê từng report service cụ thể.
/// </summary>
public static class ReportsModuleRegistration
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        // Đây là adapter ở composition root: CurrentUserService đọc claims HTTP,
        // còn Reporting chỉ biết port IReportActorContext và không phụ thuộc API.
        services.AddScoped<IReportActorContext>(provider => provider.GetRequiredService<InternalMaintenance.Api.Services.CurrentUserService>());
        return services;
    }
}
