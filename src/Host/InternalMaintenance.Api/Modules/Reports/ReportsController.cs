using InternalMaintenance.Reporting.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.Reports;

// Controller chỉ nhận request, kiểm tra dữ liệu HTTP và trả response. Các báo cáo tổng hợp xuyên module được đặt trong Services để gom persistence vào ranh giới Reporting.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
public class ReportsController : ControllerBase
{
    private readonly IReportExportOrchestrator _exportOrchestrator;
    private readonly ISlaComplianceReportService _slaComplianceReportService;
    private readonly ITechnicianPerformanceReportService _technicianPerformanceReportService;
    private readonly IMaintenanceCostReportService _maintenanceCostReportService;

    public ReportsController(
        IReportExportOrchestrator exportOrchestrator,
        ISlaComplianceReportService slaComplianceReportService,
        ITechnicianPerformanceReportService technicianPerformanceReportService,
        IMaintenanceCostReportService maintenanceCostReportService)
    {
        _exportOrchestrator = exportOrchestrator;
        _slaComplianceReportService = slaComplianceReportService;
        _technicianPerformanceReportService = technicianPerformanceReportService;
        _maintenanceCostReportService = maintenanceCostReportService;
    }

    private string? ValidateFilter(ReportFilterQuery filter)
    {
        // Khoảng ngày đảo ngược thường tạo báo cáo rỗng hợp lệ về mặt SQL nhưng sai về ý nghĩa nghiệp vụ;
        // từ chối ngay tại HTTP boundary để người dùng sửa bộ lọc thay vì hiểu nhầm là không có dữ liệu.
        if (filter.FromDate.HasValue && filter.ToDate.HasValue &&
            filter.FromDate.Value.Date > filter.ToDate.Value.Date)
            return "'Từ ngày' không thể sau 'Đến ngày'.";

        if (filter.FromDate.HasValue && filter.FromDate.Value.Date > DateTime.UtcNow.Date)
            return "'Từ ngày' không thể nằm trong tương lai.";

        if (filter.ToDate.HasValue && filter.ToDate.Value.Date > DateTime.UtcNow.Date)
            return "'Đến ngày' không thể nằm trong tương lai.";

        // ID bằng 0 hoặc âm không thể là khóa hợp lệ; trả 400 rõ ràng thay vì tạo báo cáo rỗng khó chẩn đoán.
        if (filter.DepartmentId is <= 0)
            return "DepartmentId phải lớn hơn 0.";

        if (filter.TechnicianId is <= 0)
            return "TechnicianId phải lớn hơn 0.";

        return null;
    }

    [HttpGet("technician-performance")]
    public async Task<ActionResult<List<TechnicianPerformanceReportResponse>>> GetTechnicianPerformance(
        [FromQuery] ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateFilter(filter);
        if (validation != null) return BadRequest(new { message = validation });

        // Reporting query service lọc và nhóm trong SQL; controller chỉ xử lý HTTP và validation.
        var result = await _technicianPerformanceReportService.GetAsync(filter, cancellationToken);
        return Ok(result.ToList());
    }
    [HttpGet("sla-compliance")]
    public async Task<ActionResult<SlaComplianceReportResponse>> GetSlaCompliance(
        [FromQuery] ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateFilter(filter);
        if (validation != null) return BadRequest(new { message = validation });
        // Reporting service chạy filter/group/count trong SQL; controller chỉ chuyển HTTP input/output.
        var report = await _slaComplianceReportService.GetAsync(filter, cancellationToken);
        return Ok(report);
    }

    [HttpGet("maintenance-cost")]
    public async Task<ActionResult<MaintenanceCostReportResponse>> GetMaintenanceCost(
        [FromQuery] ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateFilter(filter);
        if (validation != null) return BadRequest(new { message = validation });

        // Controller kiểm tra HTTP input; Reports service chịu trách nhiệm ghép và tổng hợp dữ liệu các module.
        var report = await _maintenanceCostReportService.GetAsync(filter, cancellationToken);
        return Ok(report);
    }
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateFilter(filter);
        if (validation != null) return BadRequest(new { message = validation });

        // Export gọi use case báo cáo trực tiếp thay vì gọi action HTTP và đọc ngược ActionResult.
        var fileBytes = await _exportOrchestrator.ExportExcelAsync(filter, cancellationToken);
        var fileName = $"BaoCao_BaoTri_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
