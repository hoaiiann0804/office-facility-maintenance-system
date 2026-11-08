using System.Globalization;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Reporting.Contracts;

namespace InternalMaintenance.Reporting.Infrastructure.Features.GetSlaComplianceReport;

/// <summary>
/// [VERTICAL SLICE: GET SLA COMPLIANCE REPORT - BÁO CÁO TỶ LỆ TUÂN THỦ SLA]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/reports/sla-compliance (ReportsController)
///           -> ISlaComplianceReportService.GetAsync(filter)
///           -> GetSlaComplianceReportHandler.HandleAsync(filter)
///           -> Bước 1: Xác định phạm vi quyền hạn của người gọi (Admin xem toàn hệ thống, Manager xem theo DepartmentId).
///           -> Bước 2: Gọi sang module Maintenance qua cổng `ISlaComplianceReadStore.ReadAsync(...)` để lấy số liệu thô
///              đã tổng hợp dưới SQL (không kéo thực thể MaintenanceTicket sang module Reporting).
///           -> Bước 3: Tính toán tỷ lệ tuân thủ SLA tổng thể (`OverallComplianceRate`), phân rã theo mức độ ưu tiên (`ByPriority`)
///              và dựng chuỗi xu hướng 6 tháng gần nhất (`MonthlyTrends`).
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Đóng gói trọn vẹn công thức tính phần trăm SLA và tạo trục thời gian 6 tháng liên tục vào 1 Slice độc lập.
///    - Tuân thủ tuyệt đối ranh giới Modular Monolith: Reporting không tham chiếu `AppDbContext` mà đọc qua Read Contract.
/// </summary>
public sealed class GetSlaComplianceReportHandler(
    ISlaComplianceReadStore readStore,
    IReportActorContext currentUserService)
{
    public async Task<SlaComplianceReportResponse> HandleAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var role = currentUserService.Role;
        var departmentId = currentUserService.DepartmentId;
        var isAdministrator = role == UserRoles.Admin;
        var canViewReport = isAdministrator || role == UserRoles.Manager && departmentId.HasValue;
        var now = DateTime.UtcNow;

        var data = await readStore.ReadAsync(
            new SlaComplianceReadQuery(
                filter.FromDate,
                filter.ToDate,
                filter.DepartmentId,
                filter.TechnicianId,
                isAdministrator,
                departmentId,
                canViewReport,
                now),
            cancellationToken);

        var metSla = data.MetSlaCount;
        var missedSla = data.MissedSlaCount;
        var finished = metSla + missedSla;

        var byPriority = data.PriorityRows.Select(row =>
        {
            var priorityFinished = row.Met + row.Missed;
            return new SlaPriorityBreakdown
            {
                Priority = row.Priority,
                TotalTickets = row.Total,
                MetSlaCount = row.Met,
                MissedSlaCount = row.Missed,
                ComplianceRate = CalculateCompliance(row.Met, priorityFinished)
            };
        }).ToList();

        var monthlyRows = data.MonthlyRows.ToDictionary(
            item => (item.Year, item.Month),
            item => (item.Met, item.Missed));

        var monthlyTrends = new List<MonthlySlaTrend>(capacity: 6);
        for (var monthOffset = 5; monthOffset >= 0; monthOffset--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-monthOffset);
            var (monthMet, monthMissed) = monthlyRows.GetValueOrDefault((month.Year, month.Month));
            var monthFinished = monthMet + monthMissed;
            monthlyTrends.Add(new MonthlySlaTrend
            {
                Month = month.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                TotalResolved = monthFinished,
                MetSlaCount = monthMet,
                MissedSlaCount = monthMissed,
                ComplianceRate = CalculateCompliance(monthMet, monthFinished)
            });
        }

        return new SlaComplianceReportResponse
        {
            TotalTickets = data.TotalTickets,
            InSlaActiveCount = data.InSlaActiveCount,
            NearBreachCount = data.NearBreachCount,
            BreachedActiveCount = data.BreachedActiveCount,
            PausedCount = data.PausedCount,
            MetSlaCount = metSla,
            MissedSlaCount = missedSla,
            OverallComplianceRate = CalculateCompliance(metSla, finished),
            ByPriority = byPriority,
            MonthlyTrends = monthlyTrends
        };
    }

    private static double CalculateCompliance(int met, int finished) =>
        finished > 0 ? Math.Round((double)met / finished * 100, 1) : 100.0;
}

