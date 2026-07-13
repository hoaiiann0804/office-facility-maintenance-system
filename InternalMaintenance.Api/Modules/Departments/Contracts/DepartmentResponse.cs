namespace InternalMaintenance.Api.Modules.Departments.Contracts;
public class DepartmentResponse
{
    public int Id{get;set;}
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}
