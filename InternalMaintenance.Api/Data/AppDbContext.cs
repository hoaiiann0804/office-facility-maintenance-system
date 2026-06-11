using Microsoft.EntityFrameworkCore;
namespace InternalMaintenance.Api.Data;
// DbContext = lớp của EF Core dùng để làm việc với database
// AppDbContext = lớp đại diện cho database trong code C#
public class AppDbContext : DbContext
{
    public AppDbContext (DbContextOptions<AppDbContext > options) : base(options)
    {
        
    }
}