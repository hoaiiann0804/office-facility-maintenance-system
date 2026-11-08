namespace InternalMaintenance.Identity.Contracts;

/// <summary>Read-only contract để kiểm tra điều kiện phân công mà không phụ thuộc entity User.</summary>
public interface ITechnicianDirectory
{
    Task<TechnicianRecord?> FindAsync(int userId, CancellationToken cancellationToken = default);
}
