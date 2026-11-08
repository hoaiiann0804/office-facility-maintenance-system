namespace InternalMaintenance.Organization.Domain;

// Department chỉ giữ dữ liệu do Organization sở hữu; module khác tham chiếu bằng DepartmentId.
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMaintenanceTeam { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
