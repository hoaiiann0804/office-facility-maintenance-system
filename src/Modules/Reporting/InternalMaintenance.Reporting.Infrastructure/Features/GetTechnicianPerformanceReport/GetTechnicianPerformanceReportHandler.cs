using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Reporting.Contracts;

namespace InternalMaintenance.Reporting.Infrastructure.Features.GetTechnicianPerformanceReport;

/// <summary>
/// [VERTICAL SLICE: GET TECHNICIAN PERFORMANCE REPORT - BÁO CÁO HIỆU SUẤT KỸ THUẬT VIÊN]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/reports/technician-performance (ReportsController)
///           -> ITechnicianPerformanceReportService.GetAsync(filter)
///           -> GetTechnicianPerformanceReportHandler.HandleAsync(filter)
///           -> Bước 1: Kiểm tra vai trò người gọi (Admin hoặc Manager có DepartmentId).
///           -> Bước 2: Gọi `ITechnicianPerformanceReadStore.ReadAsync(...)` sang module Maintenance để lấy thống kê
///              số lượng ticket được giao, đang xử lý, chờ vendor, đã giải quyết và thời gian xử lý trung bình (giây).
///           -> Bước 3: Quy đổi `AverageResolutionSeconds` sang giờ (`AvgResolutionHours`) và tính tỷ lệ đạt SLA
///              của từng kỹ thuật viên.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Tách biệt hoàn toàn logic tính hiệu suất kỹ thuật viên vào 1 Slice, chỉ phụ thuộc vào
///      `ITechnicianPerformanceReadStore` và `IReportActorContext`.
/// </summary>
public sealed class GetTechnicianPerformanceReportHandler(
    ITechnicianPerformanceReadStore readStore,
    IReportActorContext currentUserService)
{
    public async Task<IReadOnlyCollection<TechnicianPerformanceReportResponse>> HandleAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var roleName = currentUserService.Role;
        var managerDepartmentId = currentUserService.DepartmentId;
        var isAdministrator = roleName == UserRoles.Admin;
        var canViewReport = isAdministrator || roleName == UserRoles.Manager && managerDepartmentId.HasValue;

        var rows = await readStore.ReadAsync(
            new TechnicianPerformanceReadQuery(
                filter.FromDate,
                filter.ToDate,
                filter.DepartmentId,
                filter.TechnicianId,
                isAdministrator,
                managerDepartmentId,
                canViewReport),
            cancellationToken);

        return rows.Select(row =>
        {
            var finished = row.MetSlaCount + row.MissedSlaCount;
            return new TechnicianPerformanceReportResponse
            {
                TechnicianId = row.TechnicianId,
                FullName = row.FullName,
                Email = row.Email,
                DepartmentName = row.DepartmentName ?? "N/A",
                TotalAssigned = row.TotalAssigned,
                InProgressCount = row.InProgressCount,
                WaitingForVendorCount = row.WaitingForVendorCount,
                TotalResolved = row.ResolvedCount,
                TotalClosed = row.ClosedCount,
                MetSlaCount = row.MetSlaCount,
                MissedSlaCount = row.MissedSlaCount,
                SlaComplianceRate = finished > 0
                    ? Math.Round((double)row.MetSlaCount / finished * 100, 1)
                    : 100.0,
                AvgResolutionHours = Math.Round((row.AverageResolutionSeconds ?? 0) / 3600.0, 1)
            };
        }).ToArray();
    }
}

