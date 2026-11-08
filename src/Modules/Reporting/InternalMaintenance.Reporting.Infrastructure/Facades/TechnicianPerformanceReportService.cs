using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.GetTechnicianPerformanceReport;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Reporting.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BÁO CÁO HIỆU SUẤT KỸ THUẬT VIÊN]
/// Điều phối lời gọi từ `ITechnicianPerformanceReportService` xuống `GetTechnicianPerformanceReportHandler`.
/// </summary>
public sealed class TechnicianPerformanceReportService : ITechnicianPerformanceReportService
{
    private readonly GetTechnicianPerformanceReportHandler _handler;

    [ActivatorUtilitiesConstructor]
    public TechnicianPerformanceReportService(GetTechnicianPerformanceReportHandler handler)
    {
        _handler = handler;
    }

    public TechnicianPerformanceReportService(
        ITechnicianPerformanceReadStore readStore,
        IReportActorContext currentUserService)
        : this(new GetTechnicianPerformanceReportHandler(readStore, currentUserService))
    {
    }

    public Task<IReadOnlyCollection<TechnicianPerformanceReportResponse>> GetAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default) =>
        _handler.HandleAsync(filter, cancellationToken);
}
