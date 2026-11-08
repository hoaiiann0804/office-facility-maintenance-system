using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Infrastructure.Features.Dashboard;
using InternalMaintenance.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>
/// [THIN FAÇADE - THỐNG KÊ DASHBOARD CỦA MODULE MAINTENANCE]
/// Điều phối lời gọi từ `IMaintenanceDashboardReader` xuống `GetMaintenanceDashboardHandler`.
/// </summary>
public sealed class MaintenanceDashboardReader : IMaintenanceDashboardReader
{
    private readonly GetMaintenanceDashboardHandler _handler;

    [ActivatorUtilitiesConstructor]
    public MaintenanceDashboardReader(GetMaintenanceDashboardHandler handler)
    {
        _handler = handler;
    }

    public MaintenanceDashboardReader(AppDbContext db)
        : this(new GetMaintenanceDashboardHandler(db))
    {
    }

    public Task<MaintenanceDashboardStatistics> GetAsync(
        TicketDashboardScope scope,
        CancellationToken cancellationToken = default) =>
        _handler.HandleAsync(scope, cancellationToken);
}
