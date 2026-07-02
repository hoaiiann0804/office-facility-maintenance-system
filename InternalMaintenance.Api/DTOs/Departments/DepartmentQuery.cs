using InternalMaintenance.Api.DTOs.Common;

namespace InternalMaintenance.Api.DTOs.Departments;

public class DepartmentQuery : PaginationQuery
{
    public string? Keyword { get; set; }
}