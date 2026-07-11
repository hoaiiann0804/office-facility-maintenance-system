    using InternalMaintenance.Api.DTOs.Common;

namespace InternalMaintenance.Api.Common;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        PaginationQuery pagination)
    {
        return query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize);
    }

    public static PagedResponse<T> ToPagedResponse<T>(
        this IReadOnlyCollection<T> items,
        PaginationQuery pagination,
        int totalItems)
    {
        return new PagedResponse<T>
        {
            Items = items.ToList(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pagination.PageSize)
        };
    }
}
