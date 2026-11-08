using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure.Features.GetSlaComplianceReport;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Reporting.Infrastructure;

/// <summary>
/// [THIN FAÇADE - BÁO CÁO TUÂN THỦ SLA]
/// Điều phối lời gọi từ `ISlaComplianceReportService` xuống `GetSlaComplianceReportHandler`.
/// Giữ constructor tương thích ngược phục vụ các bài Unit Test trong `Reporting.Tests`.
/// </summary>
public sealed class SlaComplianceReportService : ISlaComplianceReportService
{
    private readonly GetSlaComplianceReportHandler _handler;

    [ActivatorUtilitiesConstructor]
    public SlaComplianceReportService(GetSlaComplianceReportHandler handler)
    {
        _handler = handler;
    }

    public SlaComplianceReportService(
        ISlaComplianceReadStore readStore,
        IReportActorContext currentUserService)
        : this(new GetSlaComplianceReportHandler(readStore, currentUserService))
    {
    }

    public Task<SlaComplianceReportResponse> GetAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default) =>
        _handler.HandleAsync(filter, cancellationToken);
}
