namespace InternalMaintenance.BuildingBlocks.Pagination;

/// <summary>
/// Tham số phân trang dùng chung cho các query danh sách.
///
/// [LƯU Ý MINIMAL API + [AsParameters]]:
/// Minimal API KHÔNG dùng giá trị khởi tạo của property (`= 1`, `= 10`) để xác định tham số tùy chọn.
/// Property kiểu `int` (non-nullable) sẽ bị coi là BẮT BUỘC trên query string, dẫn tới lỗi
/// `Required parameter "int Page" was not provided from query string` khi client không gửi `?page=`.
/// Vì vậy `Page`/`PageSize` khai báo `int?` để binding là tùy chọn, còn code nghiệp vụ đọc
/// `ResolvedPage`/`ResolvedPageSize` (get-only nên [AsParameters] bỏ qua) để luôn có giá trị hợp lệ.
/// </summary>
public class PaginationQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;

    public int? Page { get; set; } = DefaultPage;

    public int? PageSize { get; set; } = DefaultPageSize;

    /// <summary>Trang hiện tại đã chuẩn hóa (thiếu hoặc &lt;= 0 thì về trang 1).</summary>
    public int ResolvedPage => Page is > 0 ? Page.Value : DefaultPage;

    /// <summary>Kích thước trang đã chuẩn hóa (thiếu hoặc &lt;= 0 thì về 10).</summary>
    public int ResolvedPageSize => PageSize is > 0 ? PageSize.Value : DefaultPageSize;
}
