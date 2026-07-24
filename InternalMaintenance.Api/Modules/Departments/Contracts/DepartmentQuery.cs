using InternalMaintenance.Api.Common.Pagination;

namespace InternalMaintenance.Api.Modules.Departments.Contracts;

public class DepartmentQuery : PaginationQuery
{
    public string? Keyword { get; set; }
    public bool? IsMaintenanceTeam { get; set; }
}
