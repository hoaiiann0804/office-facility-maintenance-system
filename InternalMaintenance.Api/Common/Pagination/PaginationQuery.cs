namespace InternalMaintenance.Api.Common.Pagination;

public class PaginationQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
