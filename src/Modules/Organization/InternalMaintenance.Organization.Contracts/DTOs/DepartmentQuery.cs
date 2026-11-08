using InternalMaintenance.BuildingBlocks.Pagination;

namespace InternalMaintenance.Organization.Contracts;

public class DepartmentQuery : PaginationQuery
{
    public string? Keyword { get; set; }
    public bool? IsMaintenanceTeam { get; set; }
}
