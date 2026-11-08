using InternalMaintenance.Persistence;
using InternalMaintenance.Organization.Domain;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Organization.Infrastructure;

/// <summary>
/// Seed Department do Organization sở hữu. Host gọi hàm này trước các module tham chiếu DepartmentId,
/// đồng thời truyền cùng scoped AppDbContext để giữ nguyên thứ tự khởi tạo và transaction hiện tại.
/// </summary>
public static class OrganizationSeedData
{
    public static async Task SeedDepartmentsAsync(AppDbContext context)
    {
        // Giữ hành vi cũ: nếu đã có Department thì không thêm bộ dữ liệu mẫu lần nữa.
        if (await context.Departments.AnyAsync())
            return;

        context.Departments.AddRange(
            new Department
            {
                Name = "IT",
                Description = "Information Technology Department",
                IsMaintenanceTeam = true
            },
            new Department
            {
                Name = "Accounting",
                Description = "Accounting Department",
                IsMaintenanceTeam = false
            },
            new Department
            {
                Name = "HR",
                Description = "Human Resources Department",
                IsMaintenanceTeam = false
            });

        await context.SaveChangesAsync();
    }
}
