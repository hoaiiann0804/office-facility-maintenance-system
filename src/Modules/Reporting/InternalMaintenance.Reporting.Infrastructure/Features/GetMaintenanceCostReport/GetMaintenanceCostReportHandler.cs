using System.Globalization;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Vendors.Contracts;

namespace InternalMaintenance.Reporting.Infrastructure.Features.GetMaintenanceCostReport;

/// <summary>
/// [VERTICAL SLICE: GET MAINTENANCE COST REPORT - BÁO CÁO CHI PHÍ BẢO TRÌ &amp; ĐỐI TÁC SỬA CHỮA]
///
/// 1. LUỒNG ĐI DỮ LIỆU (REQUEST FLOW):
///    Client -> GET /api/reports/maintenance-cost (ReportsController)
///           -> IMaintenanceCostReportService.GetAsync(filter)
///           -> GetMaintenanceCostReportHandler.HandleAsync(filter)
///           -> Bước 1: Xác định quyền xem báo cáo theo Role/DepartmentId.
///           -> Bước 2: Gọi `IMaintenanceCostReadStore.ReadAsync(...)` lấy dữ liệu tổng hợp chi phí theo tháng,
///              theo phòng ban, theo thiết bị tốn kém nhất và theo `VendorId`.
///           -> Bước 3 (Giao tiếp liên module qua Contract):
///              Gọi `IVendorDirectory.FindNamesAsync(vendorIds)` sang module `Vendors` để lấy tên nhà cung cấp
///              thay vì join trực tiếp bảng `Vendors`.
///           -> Bước 4: Tính chi phí trung bình mỗi ticket (`AvgCostPerTicket`) và trả về `MaintenanceCostReportResponse`.
///
/// 2. CÁCH GIẢI QUYẾT VẤN ĐỀ KHI TÁI CẤU TRÚC:
///    - Chỉ duy nhất Slice báo cáo chi phí này mới cần tiêm `IVendorDirectory` (2 Slice báo cáo còn lại không cần).
/// </summary>
public sealed class GetMaintenanceCostReportHandler(
    IMaintenanceCostReadStore readStore,
    IReportActorContext currentUserService,
    IVendorDirectory vendorDirectory)
{
    public async Task<MaintenanceCostReportResponse> HandleAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var role = currentUserService.Role;
        var managerDepartmentId = currentUserService.DepartmentId;
        var isAdministrator = role == UserRoles.Admin;
        var canViewReport = isAdministrator || role == UserRoles.Manager && managerDepartmentId.HasValue;
        var now = DateTime.UtcNow;

        var data = await readStore.ReadAsync(
            new MaintenanceCostReadQuery(
                filter.FromDate,
                filter.ToDate,
                filter.DepartmentId,
                isAdministrator,
                managerDepartmentId,
                canViewReport,
                now),
            cancellationToken);

        var vendorIds = data.VendorCosts.Select(item => item.VendorId).ToArray();
        var vendorNames = await vendorDirectory.FindNamesAsync(vendorIds, cancellationToken);

        var vendorCosts = data.VendorCosts
            .Select(item => new VendorCostItem
            {
                VendorId = item.VendorId,
                VendorName = vendorNames.GetValueOrDefault(item.VendorId, "Vendor không còn trong danh mục"),
                TotalCost = item.TotalCost,
                DispatchCount = item.DispatchCount
            })
            .Where(item => item.TotalCost > 0 || item.DispatchCount > 0)
            .OrderByDescending(item => item.TotalCost)
            .ToList();

        var monthlyRows = data.MonthlyCosts.ToDictionary(
            item => (item.Year, item.Month),
            item => item);

        var monthlyCosts = new List<MonthlyCostItem>(capacity: 6);
        for (var monthOffset = 5; monthOffset >= 0; monthOffset--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-monthOffset);
            var row = monthlyRows.GetValueOrDefault((month.Year, month.Month));
            monthlyCosts.Add(new MonthlyCostItem
            {
                Month = month.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                TotalCost = row?.TotalCost ?? 0m,
                TicketCount = row?.TicketCount ?? 0
            });
        }

        var topEquipment = data.TopEquipment.Select(item => new CostlyEquipmentItem
        {
            EquipmentId = item.EquipmentId,
            EquipmentCode = item.EquipmentCode,
            EquipmentName = item.EquipmentName,
            DepartmentName = item.DepartmentName ?? "N/A",
            TotalRepairCost = item.TotalRepairCost,
            MaintenanceCount = item.MaintenanceCount
        }).ToList();

        var departmentCosts = data.DepartmentCosts
            .Where(item => item.TotalCost > 0 || item.TicketCount > 0)
            .Select(item => new DepartmentCostItem
            {
                DepartmentId = item.DepartmentId,
                DepartmentName = item.DepartmentName ?? "N/A",
                TotalCost = item.TotalCost,
                TicketCount = item.TicketCount
            })
            .OrderByDescending(item => item.TotalCost)
            .ToList();

        return new MaintenanceCostReportResponse
        {
            TotalMaintenanceCost = data.TotalCost,
            TotalVendorDispatches = data.TotalDispatches,
            AvgCostPerTicket = data.DistinctTicketCount > 0
                ? data.TotalCost / data.DistinctTicketCount
                : 0,
            MonthlyCosts = monthlyCosts,
            DepartmentCosts = departmentCosts,
            VendorCosts = vendorCosts,
            TopCostlyEquipment = topEquipment
        };
    }
}

