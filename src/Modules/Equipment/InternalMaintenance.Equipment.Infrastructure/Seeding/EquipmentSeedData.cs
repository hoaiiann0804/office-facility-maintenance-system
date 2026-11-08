using InternalMaintenance.Persistence;
using InternalMaintenance.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using AssetsEquipment = InternalMaintenance.Assets.Domain.Equipment;

namespace InternalMaintenance.Assets.Infrastructure;

/// <summary>
/// Dữ liệu khởi tạo thiết bị thuộc module Equipment. Host chỉ gọi entry point này sau khi
/// Organization đã seed Department; việc dùng cùng AppDbContext giữ nguyên FK và transaction hiện tại.
/// </summary>
public static class EquipmentSeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var accountingDepartment = await context.Departments
            .FirstOrDefaultAsync(department => department.Name == "Accounting");
        var itDepartment = await context.Departments
            .FirstOrDefaultAsync(department => department.Name == "IT");

        // Không tạo Equipment mồ côi nếu dữ liệu Department nền chưa được khởi tạo.
        if (accountingDepartment is null || itDepartment is null)
            return;

        await EnsureEquipmentAsync(
            context,
            code: "PRN-ACC-001",
            name: "Canon Printer - Accounting Room",
            departmentId: accountingDepartment.Id,
            maintenanceDepartmentId: itDepartment.Id,
            purchasedDate: new DateTime(2025, 1, 10),
            description: "Main printer used by accounting department");

        await EnsureEquipmentAsync(
            context,
            code: "RTR-IT-001",
            name: "Main Office Router",
            departmentId: itDepartment.Id,
            maintenanceDepartmentId: itDepartment.Id,
            purchasedDate: new DateTime(2024, 8, 15),
            description: "Router used for internal office network");

        await context.SaveChangesAsync();
    }

    private static async Task EnsureEquipmentAsync(
        AppDbContext context,
        string code,
        string name,
        int departmentId,
        int? maintenanceDepartmentId,
        DateTime purchasedDate,
        string description)
    {
        var equipment = await context.Equipment
            .FirstOrDefaultAsync(item => item.Code == code);

        if (equipment is null)
        {
            context.Equipment.Add(new AssetsEquipment
            {
                Code = code,
                Name = name,
                DepartmentId = departmentId,
                MaintenanceDepartmentId = maintenanceDepartmentId,
                Status = "Active",
                PurchasedDate = purchasedDate,
                Description = description
            });
            return;
        }

        // Giữ nguyên Id/status của thiết bị đã tồn tại; seed chỉ đồng bộ các trường cấu hình ban đầu.
        equipment.DepartmentId = departmentId;
        equipment.MaintenanceDepartmentId = maintenanceDepartmentId ?? equipment.MaintenanceDepartmentId;
        equipment.PurchasedDate = purchasedDate;
        equipment.Description = description;
    }
}
