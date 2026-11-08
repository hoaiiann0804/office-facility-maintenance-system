using ClosedXML.Excel;
using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure;

namespace Reporting.Tests;

public sealed class ReportExportServiceTests
{
    [Fact]
    public void Export_includes_applied_filters_and_vendor_dispatch_breakdown()
    {
        var exporter = new ReportExportService();
        var bytes = exporter.ExportAllReportsToExcel(
            [],
            new SlaComplianceReportResponse
            {
                ByPriority = [new SlaPriorityBreakdown { Priority = "High", TotalTickets = 1 }]
            },
            new MaintenanceCostReportResponse
            {
                AvgCostPerTicket = 150m,
                DepartmentCosts =
                [
                    new DepartmentCostItem
                    {
                        DepartmentId = 7,
                        DepartmentName = "IT",
                        TicketCount = 1,
                        TotalCost = 150m
                    }
                ],
                TopCostlyEquipment =
                [
                    new CostlyEquipmentItem
                    {
                        EquipmentId = 9,
                        EquipmentCode = "EQ-9",
                        EquipmentName = "Laptop",
                        DepartmentName = "IT",
                        MaintenanceCount = 1,
                        TotalRepairCost = 150m
                    }
                ],
                VendorCosts =
                [
                    new VendorCostItem
                    {
                        VendorId = 3,
                        VendorName = "Vendor A",
                        DispatchCount = 2,
                        TotalCost = 150m
                    }
                ]
            },
            new ReportFilterQuery { DepartmentId = 7, TechnicianId = 42 });

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var slaWorksheet = workbook.Worksheet(2);
        var costWorksheet = workbook.Worksheet(3);

        // File Excel phải giữ filter được người dùng chọn, nếu không người nhận dễ hiểu nhầm số liệu là toàn tổ chức.
        Assert.Contains("DepartmentId: 7", costWorksheet.Cell("A2").GetString());
        Assert.Contains("TechnicianId: 42", costWorksheet.Cell("A2").GetString());

        // Breakdown vendor dùng dispatch count; cột ticket và chi phí trung bình có nhãn đúng ý nghĩa dữ liệu.
        Assert.Equal("Ticket Có Kết Quả SLA", slaWorksheet.Cell(10, 2).GetString());
        Assert.Equal("Chi Phí Trung Bình / Ticket:", costWorksheet.Cell("A6").GetString());
        Assert.Equal("Vendor A", costWorksheet.Cell(20, 1).GetString());
        Assert.Equal(2, costWorksheet.Cell(20, 2).GetValue<int>());
        Assert.Equal(150d, costWorksheet.Cell(20, 3).GetValue<double>());
    }
}
