using InternalMaintenance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();
        if(!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "Manager" },
                new Role { Name = "Staff" },
                new Role { Name = "Technician" }
            );
            await context.SaveChangesAsync();
        }
        if(!await context.Departments.AnyAsync())
        {
            context.Departments.AddRange(
                new Department{Name= "IT", Description = "Information Technology Department"},
                new Department { Name = "Accounting", Description = "Accounting Department" },
                new Department { Name = "HR", Description = "Human Resources Department" }
            );
            await context.SaveChangesAsync();
        }
        if(!await context.Equipment.AnyAsync())
        {
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
        }
        await context.SaveChangesAsync();
    }
}