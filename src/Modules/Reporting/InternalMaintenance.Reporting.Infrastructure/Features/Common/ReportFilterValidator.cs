using InternalMaintenance.Reporting.Contracts;

namespace InternalMaintenance.Reporting.Infrastructure.Features.Common;

/// <summary>
/// [REPORT FILTER VALIDATOR - KIỂM TRA BỘ LỌC BÁO CÁO TẠI BIÊN HTTP]
/// Gom các quy tắc kiểm tra khoảng ngày và ID của bộ lọc báo cáo để tái sử dụng nhất quán
/// giữa tất cả các Minimal API Endpoints trong module Reporting.
/// </summary>
public static class ReportFilterValidator
{
    public static string? Validate(ReportFilterQuery filter)
    {
        if (filter.FromDate.HasValue && filter.ToDate.HasValue &&
            filter.FromDate.Value.Date > filter.ToDate.Value.Date)
            return "'Từ ngày' không thể sau 'Đến ngày'.";

        if (filter.FromDate.HasValue && filter.FromDate.Value.Date > DateTime.UtcNow.Date)
            return "'Từ ngày' không thể nằm trong tương lai.";

        if (filter.ToDate.HasValue && filter.ToDate.Value.Date > DateTime.UtcNow.Date)
            return "'Đến ngày' không thể nằm trong tương lai.";

        if (filter.DepartmentId is <= 0)
            return "DepartmentId phải lớn hơn 0.";

        if (filter.TechnicianId is <= 0)
            return "TechnicianId phải lớn hơn 0.";

        return null;
    }
}

