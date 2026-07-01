using InternalMaintenance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Data;

public static class SeedData
{
    private const string TemporaryPassword = "Temp@123456";
    public static async Task InitializeAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();
        await SeedRolesAsync(context);
        await SeedDepartmentsAsync(context);
        await SeedEquipmentAsync(context);
        await SeedAuthUsersAsync(context);
    }
    private static async Task SeedRolesAsync(AppDbContext context)
    {
        await EnsureRoleAsync ( context,"Admin" );
        await EnsureRoleAsync ( context, "Manager" );
        await EnsureRoleAsync ( context, "Staff" );
        await EnsureRoleAsync ( context, "Technician");
        await context.SaveChangesAsync();
        }

    private static async Task EnsureRoleAsync (AppDbContext context , string roleName)
    {
        var roleExists = await context.Roles
        .AnyAsync(role => role.Name == roleName);
        if(roleExists)
        {
            return;
        }
        context.Roles.Add( new Role
        {
            Name = roleName
        });
        
    }

    private static async Task SeedDepartmentsAsync (AppDbContext context)
    {
        if(await context.Departments.AnyAsync())
        {
            return;
        }

          context.Departments.AddRange(
            new Department
            {
                Name = "IT",
                Description = "Information Technology Department"
            },
            new Department
            {
                Name = "Accounting",
                Description = "Accounting Department"
            },
            new Department
            {
                Name = "HR",
                Description = "Human Resources Department"
            }
        );
        await context.SaveChangesAsync();
        
    }
    private static async Task SeedEquipmentAsync (AppDbContext context)
    {
        if(await context.Equipment.AnyAsync())
        {
            return;
        }
        var accountingDepartment = await context.Departments.FirstAsync(d=> d.Name =="Accounting");
        var itDepartment = await context.Departments.FirstAsync(d=>d.Name == "IT");

        context.Equipment.AddRange(
                new Equipment
                {
                    Code = "PRN-ACC-001",
                    Name = "Canon Printer - Accounting Room",
                    DepartmentId = accountingDepartment.Id,
                    Status = "Active",
                    PurchasedDate = new DateTime(2025, 1, 10),
                    Description = "Main printer used by accounting department"
                },
                new Equipment
                { Code = "RTR-IT-001",
                    Name = "Main Office Router",
                    DepartmentId = itDepartment.Id,
                    Status = "Active",
                    PurchasedDate = new DateTime(2024, 8, 15),
                    Description = "Router used for internal office network"
                }
            );
            
        await context.SaveChangesAsync();
        }

    public static async Task SeedAuthUsersAsync (AppDbContext context)
    {
        var adminRole = await context.Roles
        .FirstAsync(role => role.Name == "Admin");

        var managerRole = await context.Roles
        .FirstAsync(role => role.Name == "Manager");

        var staffRole = await context.Roles
        .FirstAsync(role => role.Name == "Staff");

        var technicianRole = await context.Roles
        .FirstAsync(role => role.Name=="Technician");

        var itDepartment = await context.Departments
        .FirstOrDefaultAsync(department => department.Name=="IT");

         var accountingDepartment  = await context.Departments
        .FirstOrDefaultAsync(department => department.Name=="Accounting");

        await EnsureUserAsync(
            context: context,
            fullName: "System Admin",
            email: "admin@test.com",
            roleId: adminRole.Id,
            departmentId: null
        );

         await EnsureUserAsync(
            context: context,
            fullName: "Manager Test",
            email: "manager@test.com",
            roleId: managerRole.Id,
            departmentId: itDepartment?.Id
        );
         await EnsureUserAsync(
            context: context,
            fullName: "Staff Test",
            email: "staff@test.com",
            roleId: staffRole.Id,
            departmentId: accountingDepartment?.Id
        );
          await EnsureUserAsync(
            context: context,
            fullName: "Technician Test",
            email: "technician@test.com",
            roleId: technicianRole.Id,
            departmentId: itDepartment?.Id
        );
        await context.SaveChangesAsync();   
    }
    private static async Task EnsureUserAsync(
        AppDbContext context,
        string fullName,
        string email,
        int roleId,
        int? departmentId)
    {
        var user = await context.Users
        .FirstOrDefaultAsync(existingUser => existingUser.Email == email);

        if(user is null)
        {
            context.Users.Add(
                new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(TemporaryPassword),
                    RoleId = roleId,
                    DepartmentId = departmentId,
                    IsActive = true,
                    MustChangePassword = true,
                    LastLoginAt= null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                }
            );
            return;
        }
        //User đã tồn tại từ dữ liệu cũ
        //Giữ nguyên Id không ảnh hưởng các ticket đang liên kết với user này.
        user.FullName = string.IsNullOrWhiteSpace(user.FullName)
        ? fullName : user.FullName;

        user.RoleId = roleId;
        user.DepartmentId = departmentId;

        // Không tự động set IsActive = true cho user đã tồn tại,
        // Vì nếu Admin khóa user thì seed không nên tự mở lại 
        // user.IsActive = true;

        // CHỉ cập nhật password cũ chưa phải BCrypt hash.
        // việc này tránh reset password mỗi lần app chạy
        if(!IsBCryptHash(user.PasswordHash))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(TemporaryPassword);
            user.MustChangePassword = true;
        }
        user.UpdatedAt = DateTime.UtcNow;
    }
    private static bool IsBCryptHash (string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }
         return passwordHash.StartsWith("$2a$")
            || passwordHash.StartsWith("$2b$")
            || passwordHash.StartsWith("$2y$");
    }

}