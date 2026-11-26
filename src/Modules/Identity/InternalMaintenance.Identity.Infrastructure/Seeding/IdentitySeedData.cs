using InternalMaintenance.Persistence;
using InternalMaintenance.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Identity.Infrastructure;

/// <summary>
/// Identity sở hữu dữ liệu mẫu Role/User. DepartmentId được lưu dạng scalar để Identity không
/// tham chiếu entity Organization; host chỉ bảo đảm Department đã seed trước khi gọi SeedUsersAsync.
/// </summary>
public static class IdentitySeedData
{
    private const string DevTemporaryPassword = "Temp@123456";
    private const string ProdDemoPassword = "Demo@Prod2026!";

    public static async Task SeedRolesAsync(AppDbContext context)
    {
        await EnsureRoleAsync(context, "Admin");
        await EnsureRoleAsync(context, "Manager");
        await EnsureRoleAsync(context, "Staff");
        await EnsureRoleAsync(context, "Technician");
        await context.SaveChangesAsync();
    }

    public static async Task SeedUsersAsync(AppDbContext context, bool isProduction = false)
    {
        var adminRole = await context.Roles.FirstAsync(role => role.Name == "Admin");
        var managerRole = await context.Roles.FirstAsync(role => role.Name == "Manager");
        var staffRole = await context.Roles.FirstAsync(role => role.Name == "Staff");
        var technicianRole = await context.Roles.FirstAsync(role => role.Name == "Technician");

        var itDepartment = await context.Departments
            .FirstOrDefaultAsync(department => department.Name == "IT");
        var accountingDepartment = await context.Departments
            .FirstOrDefaultAsync(department => department.Name == "Accounting");

        // [PHÂN TÁCH TÀI KHOẢN QUICK LOGIN GIỮA DEVELOPMENT VÀ PRODUCTION]:
        // - Môi trường Development: Dùng bộ tài khoản *@test.com (mật khẩu Temp@123456).
        // - Môi trường Production: Dùng bộ tài khoản test riêng cho Nhà tuyển dụng / Người dùng trải nghiệm
        //   (demo.*@office.vn, mật khẩu Demo@Prod2026!) để vẫn bấm Quick Login 1-click trên Production
        //   nhưng tách biệt hoàn toàn với tài khoản Dev.
        if (isProduction)
        {
            await EnsureUserAsync(context, "Demo Admin (Production)", "demo.admin@office.vn", ProdDemoPassword, adminRole.Id, null);
            await EnsureUserAsync(context, "Demo Manager (Production)", "demo.manager@office.vn", ProdDemoPassword, managerRole.Id, itDepartment?.Id);
            await EnsureUserAsync(context, "Demo Staff (Production)", "demo.staff@office.vn", ProdDemoPassword, staffRole.Id, accountingDepartment?.Id);
            await EnsureUserAsync(context, "Demo Technician (Production)", "demo.tech@office.vn", ProdDemoPassword, technicianRole.Id, itDepartment?.Id);
        }
        else
        {
            await EnsureUserAsync(context, "System Admin", "admin@test.com", DevTemporaryPassword, adminRole.Id, null);
            await EnsureUserAsync(context, "Manager Test", "manager@test.com", DevTemporaryPassword, managerRole.Id, itDepartment?.Id);
            await EnsureUserAsync(context, "Staff Test", "staff@test.com", DevTemporaryPassword, staffRole.Id, accountingDepartment?.Id);
            await EnsureUserAsync(context, "Technician Test", "technician@test.com", DevTemporaryPassword, technicianRole.Id, itDepartment?.Id);
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(AppDbContext context, string roleName)
    {
        if (await context.Roles.AnyAsync(role => role.Name == roleName))
            return;

        context.Roles.Add(new Role { Name = roleName });
    }

    private static async Task EnsureUserAsync(
        AppDbContext context,
        string fullName,
        string email,
        string defaultPassword,
        int roleId,
        int? departmentId)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(existingUser => existingUser.Email == email);

        if (user is null)
        {
            context.Users.Add(new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                RoleId = roleId,
                DepartmentId = departmentId,
                IsActive = true,
                // [QUICK LOGIN 1-CLICK]: Tài khoản Quick Login của cả Dev lẫn Production đều đặt
                // MustChangePassword = false để nhà tuyển dụng/người dùng bấm 1 click là vào thẳng Dashboard.
                MustChangePassword = false,
                LastLoginAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            });
            return;
        }

        // Giữ nguyên Id để các ticket cũ vẫn trỏ đúng user; chỉ bổ sung tên nếu dữ liệu cũ đang thiếu.
        user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? fullName : user.FullName;
        user.RoleId = roleId;
        if (departmentId.HasValue)
            user.DepartmentId = departmentId;

        // Không mở khóa tài khoản Admin đã bị vô hiệu hóa. Chỉ hash lại mật khẩu cũ chưa được mã hóa.
        if (!IsBCryptHash(user.PasswordHash))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
        }

        // Đảm bảo tài khoản Quick Login không bị chặn bởi hộp thoại bắt buộc đổi mật khẩu khi demo.
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsBCryptHash(string? passwordHash) =>
        passwordHash is not null &&
        (passwordHash.StartsWith("$2a$") ||
         passwordHash.StartsWith("$2b$") ||
         passwordHash.StartsWith("$2y$"));
}
