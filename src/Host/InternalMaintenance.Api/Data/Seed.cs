using InternalMaintenance.Organization.Infrastructure;
using InternalMaintenance.Assets.Infrastructure;
using InternalMaintenance.Identity.Infrastructure;
using InternalMaintenance.Vendors.Infrastructure;
using InternalMaintenance.Persistence;

namespace InternalMaintenance.Api.Data;

/// <summary>
/// Composition root cho dữ liệu mẫu. Host chỉ sắp xếp thứ tự vì các module dùng chung database;
/// mỗi module tự sở hữu cách khởi tạo entity của mình.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // Role cần có trước khi tạo user; Department cần có trước khi gán DepartmentId cho Equipment/User.
        await IdentitySeedData.SeedRolesAsync(context);
        await OrganizationSeedData.SeedDepartmentsAsync(context);
        await EquipmentSeedData.SeedAsync(context);
        await IdentitySeedData.SeedUsersAsync(context);
        await VendorSeedData.SeedAsync(context);
    }
}
