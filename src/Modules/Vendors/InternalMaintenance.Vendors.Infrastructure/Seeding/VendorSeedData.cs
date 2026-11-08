using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Domain;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Vendors.Infrastructure;

/// <summary>
/// Dữ liệu khởi tạo của Vendor thuộc module Vendors, không đặt trong seed tổng của Host.
/// Host vẫn điều phối thời điểm gọi; module giữ dữ liệu và cách ghi vào bảng mình sở hữu.
/// </summary>
public static class VendorSeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Vendors.AnyAsync())
            return;

        context.Vendors.AddRange(
            new Vendor
            {
                Name = "Daikin Việt Nam (Trung Tâm Bảo Hành)",
                ContactPerson = "Nguyễn Văn Hùng",
                Phone = "18006777",
                Email = "baohanh@daikin.com.vn",
                Address = "184 Cao Thắng, Phường 12, Quận 10, TP.HCM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Vendor
            {
                Name = "Dell Vietnam Care Center",
                ContactPerson = "Trần Thị Mỹ",
                Phone = "1800545455",
                Email = "support@dellservice.vn",
                Address = "23 Nguyễn Thị Minh Khai, Quận 1, TP.HCM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Vendor
            {
                Name = "Samsung Service Center",
                ContactPerson = "Lê Hoàng Nam",
                Phone = "1800588889",
                Email = "services@samsung.com",
                Address = "99 Nguyễn Huệ, Quận 1, TP.HCM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Vendor
            {
                Name = "Phong Vũ Computer & Repairs",
                ContactPerson = "Phạm Quốc Tuấn",
                Phone = "18006868",
                Email = "suachua@phongvu.vn",
                Address = "264 Nguyễn Thị Minh Khai, Quận 3, TP.HCM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
    }
}
