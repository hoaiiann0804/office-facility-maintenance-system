using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.GetMaintenanceCostReport;
using InternalMaintenance.Vendors.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Reporting.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BÁO CÁO CHI PHÍ BẢO TRÌ]
/// Điều phối lời gọi từ `IMaintenanceCostReportService` xuống `GetMaintenanceCostReportHandler`.
/// </summary>
public sealed class MaintenanceCostReportService : IMaintenanceCostReportService
{
    private readonly GetMaintenanceCostReportHandler _handler;

    [ActivatorUtilitiesConstructor]
    public MaintenanceCostReportService(GetMaintenanceCostReportHandler handler)
    {
        _handler = handler;
    }

    public MaintenanceCostReportService(
        IMaintenanceCostReadStore readStore,
        IReportActorContext currentUserService,
        IVendorDirectory vendorDirectory)
        : this(new GetMaintenanceCostReportHandler(readStore, currentUserService, vendorDirectory))
    {
    }

    public Task<MaintenanceCostReportResponse> GetAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default) =>
        _handler.HandleAsync(filter, cancellationToken);
}
