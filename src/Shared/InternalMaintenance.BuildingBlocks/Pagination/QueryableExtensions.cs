namespace InternalMaintenance.BuildingBlocks.Pagination;

/// <summary>
/// Các phép phân trang thuần cho IQueryable/response; không phụ thuộc vào API host hay provider EF cụ thể.
/// </summary>
public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, PaginationQuery pagination) =>
        query.Skip((pagination.ResolvedPage - 1) * pagination.ResolvedPageSize).Take(pagination.ResolvedPageSize);

    public static PagedResponse<T> ToPagedResponse<T>(
        this IReadOnlyCollection<T> items,
        PaginationQuery pagination,
        int totalItems) => new()
        {
            Items = items.ToList(),
            Page = pagination.ResolvedPage,
            PageSize = pagination.ResolvedPageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pagination.ResolvedPageSize)
        };
}
