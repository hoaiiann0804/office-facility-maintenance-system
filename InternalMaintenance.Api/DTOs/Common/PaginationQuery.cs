namespace InternalMaintenance.Api.DTOs.Common;

public class PaginationQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}