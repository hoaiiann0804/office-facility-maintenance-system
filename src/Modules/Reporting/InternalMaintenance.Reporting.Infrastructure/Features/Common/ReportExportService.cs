using ClosedXML.Excel;
using InternalMaintenance.Reporting.Contracts;
using System.IO;

namespace InternalMaintenance.Reporting.Infrastructure;

public class ReportExportService
{
    public byte[] ExportAllReportsToExcel(
        List<TechnicianPerformanceReportResponse> techReports,
        SlaComplianceReportResponse slaReport,
        MaintenanceCostReportResponse costReport,
        ReportFilterQuery filter)
    {
        using var workbook = new XLWorkbook();

        // ─────────────────────────────────────────────────────────────
        // SHEET 1: HIỆU SUẤT KỸ THUẬT VIÊN
        // ─────────────────────────────────────────────────────────────
        var wsTech = workbook.Worksheets.Add("Hiệu Suất KTV");
        wsTech.ShowGridLines = true;

        // Title
        wsTech.Cell("A1").Value = "BÁO CÁO HIỆU SUẤT KỸ THUẬT VIÊN";
        wsTech.Cell("A1").Style.Font.Bold = true;
        wsTech.Cell("A1").Style.Font.FontSize = 14;
        wsTech.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

        // Ghi lại toàn bộ filter đã áp dụng để file export không bị hiểu nhầm là báo cáo toàn hệ thống.
        var filterParts = new List<string>
        {
            $"Thời gian: {(filter.FromDate.HasValue ? filter.FromDate.Value.ToString("dd/MM/yyyy") : "Tất cả")} - {(filter.ToDate.HasValue ? filter.ToDate.Value.ToString("dd/MM/yyyy") : "Hiện tại")}" 
        };
        if (filter.DepartmentId.HasValue)
            filterParts.Add($"DepartmentId: {filter.DepartmentId.Value}");
        if (filter.TechnicianId.HasValue)
            filterParts.Add($"TechnicianId: {filter.TechnicianId.Value}");
        var filterText = string.Join(" | ", filterParts);
        wsTech.Cell("A2").Value = filterText;
        wsTech.Cell("A2").Style.Font.Italic = true;

        // Headers
        var headersTech = new[]
        {
            "STT", "Họ và Tên", "Email", "Phòng Ban", "Được Giao", "Đang Xử Lý", "Đang Gửi Vendor",
            "Đã Xử Lý", "Đã Đóng", "Đạt SLA", "Trễ SLA", "Tỷ Lệ Đạt SLA (%)", "Thời Gian TB (Giờ)"
        };

        for (int i = 0; i < headersTech.Length; i++)
        {
            var cell = wsTech.Cell(4, i + 1);
            cell.Value = headersTech[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 5;
        int index = 1;
        foreach (var t in techReports)
        {
            wsTech.Cell(row, 1).Value = index++;
            wsTech.Cell(row, 2).Value = t.FullName;
            wsTech.Cell(row, 3).Value = t.Email;
            wsTech.Cell(row, 4).Value = t.DepartmentName;
            wsTech.Cell(row, 5).Value = t.TotalAssigned;
            wsTech.Cell(row, 6).Value = t.InProgressCount;
            wsTech.Cell(row, 7).Value = t.WaitingForVendorCount;
            wsTech.Cell(row, 8).Value = t.TotalResolved;
            wsTech.Cell(row, 9).Value = t.TotalClosed;
            wsTech.Cell(row, 10).Value = t.MetSlaCount;
            wsTech.Cell(row, 11).Value = t.MissedSlaCount;
            
            var cellRate = wsTech.Cell(row, 12);
            cellRate.Value = t.SlaComplianceRate / 100.0;
            cellRate.Style.NumberFormat.Format = "0.0%";

            var cellAvg = wsTech.Cell(row, 13);
            cellAvg.Value = t.AvgResolutionHours;
            cellAvg.Style.NumberFormat.Format = "#,##0.0";

            row++;
        }

        wsTech.Columns().AdjustToContents();

        // ─────────────────────────────────────────────────────────────
        // SHEET 2: TUÂN THỦ & VI PHẠM SLA
        // ─────────────────────────────────────────────────────────────
        var wsSla = workbook.Worksheets.Add("Tuân Thủ SLA");
        wsSla.ShowGridLines = true;

        wsSla.Cell("A1").Value = "BÁO CÁO TUÂN THỦ VÀ VI PHẠM SLA";
        wsSla.Cell("A1").Style.Font.Bold = true;
        wsSla.Cell("A1").Style.Font.FontSize = 14;
        wsSla.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

        wsSla.Cell("A2").Value = filterText;
        wsSla.Cell("A2").Style.Font.Italic = true;

        // KPI Summary Box
        wsSla.Cell("A4").Value = "Tổng Ticket:";
        wsSla.Cell("B4").Value = slaReport.TotalTickets;
        wsSla.Cell("A5").Value = "Đạt Đúng Hạn (Met SLA):";
        wsSla.Cell("B5").Value = slaReport.MetSlaCount;
        wsSla.Cell("A6").Value = "Trễ Hạn (Missed SLA):";
        wsSla.Cell("B6").Value = slaReport.MissedSlaCount;
        wsSla.Cell("A7").Value = "Tỷ Lệ Tuân Thủ Chung:";
        var cellOverall = wsSla.Cell("B7");
        cellOverall.Value = slaReport.OverallComplianceRate / 100.0;
        cellOverall.Style.NumberFormat.Format = "0.0%";
        cellOverall.Style.Font.Bold = true;

        wsSla.Range("A4:A7").Style.Font.Bold = true;

        // Table by Priority
        wsSla.Cell("A9").Value = "Phân Tích Tuân Thủ Theo Mức Độ Ưu Tiên:";
        wsSla.Cell("A9").Style.Font.Bold = true;

        // completedQuery chỉ gồm ticket Resolved/Closed có MetSLA hoặc MissedSLA;
        // đổi tên cột để không bị hiểu là tổng mọi ticket, kể cả ticket đang xử lý.
        var headersPriority = new[] { "Mức Độ Ưu Tiên", "Ticket Có Kết Quả SLA", "Đạt SLA", "Trễ SLA", "Tỷ Lệ Đạt (%)" };
        for (int i = 0; i < headersPriority.Length; i++)
        {
            var cell = wsSla.Cell(10, i + 1);
            cell.Value = headersPriority[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int rowP = 11;
        foreach (var p in slaReport.ByPriority)
        {
            wsSla.Cell(rowP, 1).Value = p.Priority;
            wsSla.Cell(rowP, 2).Value = p.TotalTickets;
            wsSla.Cell(rowP, 3).Value = p.MetSlaCount;
            wsSla.Cell(rowP, 4).Value = p.MissedSlaCount;
            var cellPrRate = wsSla.Cell(rowP, 5);
            cellPrRate.Value = p.ComplianceRate / 100.0;
            cellPrRate.Style.NumberFormat.Format = "0.0%";
            rowP++;
        }

        // Table by Month
        rowP += 2;
        wsSla.Cell(rowP, 1).Value = "Xu Hướng Xử Lý Theo Từng Tháng:";
        wsSla.Cell(rowP, 1).Style.Font.Bold = true;
        rowP++;

        var headersMonth = new[] { "Tháng", "Tổng Đã Xử Lý", "Đạt SLA", "Trễ SLA", "Tỷ Lệ Đạt (%)" };
        for (int i = 0; i < headersMonth.Length; i++)
        {
            var cell = wsSla.Cell(rowP, i + 1);
            cell.Value = headersMonth[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0284C7");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        rowP++;

        foreach (var m in slaReport.MonthlyTrends)
        {
            wsSla.Cell(rowP, 1).Value = m.Month;
            wsSla.Cell(rowP, 2).Value = m.TotalResolved;
            wsSla.Cell(rowP, 3).Value = m.MetSlaCount;
            wsSla.Cell(rowP, 4).Value = m.MissedSlaCount;
            var cellMoRate = wsSla.Cell(rowP, 5);
            cellMoRate.Value = m.ComplianceRate / 100.0;
            cellMoRate.Style.NumberFormat.Format = "0.0%";
            rowP++;
        }

        wsSla.Columns().AdjustToContents();

        // ─────────────────────────────────────────────────────────────
        // SHEET 3: CHI PHÍ BẢO DƯỠNG
        // ─────────────────────────────────────────────────────────────
        var wsCost = workbook.Worksheets.Add("Chi Phí Bảo Dưỡng");
        wsCost.ShowGridLines = true;

        wsCost.Cell("A1").Value = "BÁO CÁO TỔNG HỢP CHI PHÍ BẢO DƯỠNG THIẾT BỊ";
        wsCost.Cell("A1").Style.Font.Bold = true;
        wsCost.Cell("A1").Style.Font.FontSize = 14;
        wsCost.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

        wsCost.Cell("A2").Value = filterText;
        wsCost.Cell("A2").Style.Font.Italic = true;

        // KPI Box
        wsCost.Cell("A4").Value = "Tổng Chi Phí Bảo Dưỡng:";
        var cellTotalCost = wsCost.Cell("B4");
        cellTotalCost.Value = (double)costReport.TotalMaintenanceCost;
        cellTotalCost.Style.NumberFormat.Format = "#,##0 ₫";
        cellTotalCost.Style.Font.Bold = true;

        wsCost.Cell("A5").Value = "Tổng Lượt Gửi Đối Tác (Vendor):";
        wsCost.Cell("B5").Value = costReport.TotalVendorDispatches;

        wsCost.Cell("A6").Value = "Chi Phí Trung Bình / Ticket:";
        var cellAvgCost = wsCost.Cell("B6");
        cellAvgCost.Value = (double)costReport.AvgCostPerTicket;
        cellAvgCost.Style.NumberFormat.Format = "#,##0 ₫";

        wsCost.Range("A4:A6").Style.Font.Bold = true;

        // Department Costs Table
        wsCost.Cell("A8").Value = "Chi Phí Phân Bổ Theo Phòng Ban:";
        wsCost.Cell("A8").Style.Font.Bold = true;

        var headersDeptCost = new[] { "Phòng Ban", "Số Ticket", "Tổng Chi Phí (VNĐ)" };
        for (int i = 0; i < headersDeptCost.Length; i++)
        {
            var cell = wsCost.Cell(9, i + 1);
            cell.Value = headersDeptCost[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#7C3AED");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int rowC = 10;
        foreach (var d in costReport.DepartmentCosts)
        {
            wsCost.Cell(rowC, 1).Value = d.DepartmentName;
            wsCost.Cell(rowC, 2).Value = d.TicketCount;
            var c = wsCost.Cell(rowC, 3);
            c.Value = (double)d.TotalCost;
            c.Style.NumberFormat.Format = "#,##0 ₫";
            rowC++;
        }

        // Top Costly Equipment Table
        rowC += 2;
        wsCost.Cell(rowC, 1).Value = "Top Thiết Bị Chi Phí Sửa Chữa Cao Nhất:";
        wsCost.Cell(rowC, 1).Style.Font.Bold = true;
        rowC++;

        var headersEq = new[] { "Mã Thiết Bị", "Tên Thiết Bị", "Phòng Ban Quản Lý", "Số Ticket Bảo Trì", "Tổng Tiền Sửa (VNĐ)" };
        for (int i = 0; i < headersEq.Length; i++)
        {
            var cell = wsCost.Cell(rowC, i + 1);
            cell.Value = headersEq[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D97706");
            cell.Style.Font.FontColor = XLColor.White;
        }
        rowC++;

        foreach (var eq in costReport.TopCostlyEquipment)
        {
            wsCost.Cell(rowC, 1).Value = eq.EquipmentCode;
            wsCost.Cell(rowC, 2).Value = eq.EquipmentName;
            wsCost.Cell(rowC, 3).Value = eq.DepartmentName;
            wsCost.Cell(rowC, 4).Value = eq.MaintenanceCount;
            var c = wsCost.Cell(rowC, 5);
            c.Value = (double)eq.TotalRepairCost;
            c.Style.NumberFormat.Format = "#,##0 ₫";
            rowC++;
        }

        // Bổ sung breakdown vendor vì response đã tổng hợp sẵn; dispatch count là số lần gửi, khác với ticket count ở bảng trên.
        rowC += 2;
        wsCost.Cell(rowC, 1).Value = "Chi Phí Theo Vendor:";
        wsCost.Cell(rowC, 1).Style.Font.Bold = true;
        rowC++;

        var headersVendor = new[] { "Vendor", "Số Lượt Dispatch", "Tổng Chi Phí (VNĐ)" };
        for (int i = 0; i < headersVendor.Length; i++)
        {
            var cell = wsCost.Cell(rowC, i + 1);
            cell.Value = headersVendor[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0891B2");
            cell.Style.Font.FontColor = XLColor.White;
        }
        rowC++;

        foreach (var vendor in costReport.VendorCosts)
        {
            wsCost.Cell(rowC, 1).Value = vendor.VendorName;
            wsCost.Cell(rowC, 2).Value = vendor.DispatchCount;
            var cell = wsCost.Cell(rowC, 3);
            cell.Value = (double)vendor.TotalCost;
            cell.Style.NumberFormat.Format = "#,##0 ₫";
            rowC++;
        }

        wsCost.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
