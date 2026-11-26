using InternalMaintenance.Organization.Infrastructure;
using InternalMaintenance.Assets.Infrastructure;
using InternalMaintenance.Assets.Domain;
using InternalMaintenance.Identity.Infrastructure;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Vendors.Infrastructure;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;
using AssetsEquipment = InternalMaintenance.Assets.Domain.Equipment;

namespace InternalMaintenance.Api.Data;

/// <summary>
/// Composition root cho dữ liệu mẫu. Host chỉ sắp xếp thứ tự vì các module dùng chung database;
/// mỗi module tự sở hữu cách khởi tạo entity của mình.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(
        AppDbContext context,
        bool includeDemoData = true,
        bool isProduction = false)
    {
        // Role cơ bản luôn cần có ở mọi môi trường (Development & Production).
        await IdentitySeedData.SeedRolesAsync(context);

        if (!includeDemoData)
            return;

        await OrganizationSeedData.SeedDepartmentsAsync(context);
        await EquipmentSeedData.SeedAsync(context);
        // Truyền cờ isProduction để tạo bộ tài khoản Quick Login riêng cho Production (demo.*@office.vn)
        // tách biệt với bộ tài khoản Dev (*@test.com).
        await IdentitySeedData.SeedUsersAsync(context, isProduction);
        await VendorSeedData.SeedAsync(context);

        // [CHỈ SEED KỊCH BẢN TICKET MẪU TRÊN MÔI TRƯỜNG PRODUCTION]:
        // Ở môi trường Development (isProduction = false), giữ nguyên dữ liệu local đang có của lập trình viên.
        // Khi deploy lên Production (isProduction = true) và bảng MaintenanceTickets còn trống,
        // tự động nạp sẵn các Ticket mẫu ở đủ trạng thái (Pending, InProgress, WaitingForVendor, Closed + RepairCost)
        // để Nhà tuyển dụng / Người dùng bấm Quick Login vào là có sẵn biểu đồ và phiếu để test ngay.
        if (isProduction)
        {
            await SeedProductionSampleTicketsAsync(context);
        }
    }

    private static async Task SeedProductionSampleTicketsAsync(AppDbContext context)
    {
        if (await context.MaintenanceTickets.AnyAsync())
            return;

        var itDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "IT");
        var accDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Accounting");
        if (itDept is null || accDept is null)
            return;

        var staffUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "demo.staff@office.vn");
        var managerUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "demo.manager@office.vn");
        var techUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "demo.tech@office.vn");
        var vendor = await context.Vendors.FirstOrDefaultAsync();
        if (staffUser is null || managerUser is null || techUser is null || vendor is null)
            return;

        var printer = await context.Equipment.FirstOrDefaultAsync(e => e.Code == "PRN-ACC-001");
        var router = await context.Equipment.FirstOrDefaultAsync(e => e.Code == "RTR-IT-001");
        if (printer is null || router is null)
            return;

        // Tạo thêm 1 thiết bị điều hòa phòng Server cho kịch bản WaitingForVendor trên Production
        var serverAc = await context.Equipment.FirstOrDefaultAsync(e => e.Code == "AC-IT-002");
        if (serverAc is null)
        {
            serverAc = new AssetsEquipment
            {
                Code = "AC-IT-002",
                Name = "Daikin Inverter AC - Server Room",
                DepartmentId = itDept.Id,
                MaintenanceDepartmentId = itDept.Id,
                Status = EquipmentStatuses.UnderMaintenance,
                PurchasedDate = new DateTime(2024, 5, 20),
                Description = "Hệ thống làm mát phòng máy chủ tầng 3"
            };
            context.Equipment.Add(serverAc);
            await context.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;

        // 1. Ticket đã Closed (Đạt SLA + có chi phí sửa chữa 1.500.000đ) -> Làm giàu dữ liệu Báo cáo & Dashboard
        var closedMetTicket = MaintenanceTicket.Create(
            "TICKET-PROD-001",
            "Bảo dưỡng và thay cụm trống máy in Canon phòng Kế toán",
            "Bản in bị sọc đen dọc trang giấy khi in hóa đơn số lượng lớn.",
            printer.Id,
            staffUser.Id,
            TicketPriorities.Medium,
            now.AddDays(-5));
        closedMetTicket.Assign(techUser.Id, null);
        closedMetTicket.Start(now.AddDays(-5).AddHours(1));
        closedMetTicket.SendToVendor(now.AddDays(-5).AddHours(2));
        closedMetTicket.ReturnFromVendor(now.AddDays(-4).AddHours(2), 1440);
        closedMetTicket.Resolve("Đã thay cụm trống chính hãng và test 50 bản in sắc nét.", now.AddDays(-4).AddHours(4));
        closedMetTicket.Close(now.AddDays(-4).AddHours(5));

        // 2. Ticket đã Closed (Có chi phí sửa chữa 850.000đ) -> Làm giàu biểu đồ xu hướng chi phí
        var closedRouterTicket = MaintenanceTicket.Create(
            "TICKET-PROD-002",
            "Sửa lỗi nguồn dự phòng Router trung tâm",
            "Nguồn phụ số 2 báo đèn cam cảnh báo sụt áp.",
            router.Id,
            managerUser.Id,
            TicketPriorities.High,
            now.AddDays(-3));
        closedRouterTicket.Assign(techUser.Id, null);
        closedRouterTicket.Start(now.AddDays(-3).AddMinutes(30));
        closedRouterTicket.SendToVendor(now.AddDays(-3).AddHours(1));
        closedRouterTicket.ReturnFromVendor(now.AddDays(-2).AddHours(1), 1440);
        closedRouterTicket.Resolve("Đối tác đã thay tụ nguồn dự phòng và đo kiểm ổn định.", now.AddDays(-2).AddHours(3));
        closedRouterTicket.Close(now.AddDays(-2).AddHours(4));

        // 3. Ticket đang Pending -> Để Nhà tuyển dụng test tính năng "Giao việc"
        var pendingTicket = MaintenanceTicket.Create(
            "TICKET-PROD-003",
            "Máy in Canon phòng Kế toán báo lỗi kẹt giấy khay 2",
            "Khi in giấy định lượng dày ở khay 2 thường xuyên báo lỗi Paper Jam.",
            printer.Id,
            staffUser.Id,
            TicketPriorities.Medium,
            now.AddHours(-2));

        // 4. Ticket đang InProgress -> Để Nhà tuyển dụng test "Chuyển đơn vị ngoài" hoặc "Hoàn thành"
        var inProgressTicket = MaintenanceTicket.Create(
            "TICKET-PROD-004",
            "Cấu hình lại băng thông Wifi và kiểm tra nhiệt độ Router",
            "Tốc độ mạng nội bộ chậm vào khung giờ cao điểm buổi sáng.",
            router.Id,
            staffUser.Id,
            TicketPriorities.High,
            now.AddHours(-1));
        inProgressTicket.Assign(techUser.Id, null);
        inProgressTicket.Start(now.AddMinutes(-40));

        // 5. Ticket đang WaitingForVendor (Paused SLA) -> Để Nhà tuyển dụng thấy "Tạm dừng SLA" và nhập RepairCost
        var waitingVendorTicket = MaintenanceTicket.Create(
            "TICKET-PROD-005",
            "Máy lạnh phòng Server báo lỗi cảm biến gas U4",
            "Dàn lạnh nhấp nháy đèn xanh và không làm sâu nhiệt độ dưới 22 độ C.",
            serverAc.Id,
            managerUser.Id,
            TicketPriorities.Critical,
            now.AddHours(-6));
        waitingVendorTicket.Assign(techUser.Id, null);
        waitingVendorTicket.Start(now.AddHours(-5).AddMinutes(-45));
        waitingVendorTicket.SendToVendor(now.AddHours(-5));

        context.MaintenanceTickets.AddRange(
            closedMetTicket,
            closedRouterTicket,
            pendingTicket,
            inProgressTicket,
            waitingVendorTicket);
        await context.SaveChangesAsync();

        // Thêm nhật ký Vendor (kèm RepairCost) cho các ticket đã gửi đối tác
        context.TicketVendorLogs.AddRange(
            new TicketVendorLog
            {
                MaintenanceTicketId = closedMetTicket.Id,
                VendorId = vendor.Id,
                DepartmentIdAtDispatch = accDept.Id,
                MaintenanceDepartmentIdAtDispatch = itDept.Id,
                DispatchedAt = now.AddDays(-5).AddHours(2),
                EstimatedReturnDate = now.AddDays(-4).Date,
                ActualReturnDate = now.AddDays(-4).AddHours(2),
                PausedMinutes = 1440,
                RepairCost = 1_500_000m,
                Note = "Thay cụm trống chính hãng Canon (Bảo hành 6 tháng)"
            },
            new TicketVendorLog
            {
                MaintenanceTicketId = closedRouterTicket.Id,
                VendorId = vendor.Id,
                DepartmentIdAtDispatch = itDept.Id,
                MaintenanceDepartmentIdAtDispatch = itDept.Id,
                DispatchedAt = now.AddDays(-3).AddHours(1),
                EstimatedReturnDate = now.AddDays(-2).Date,
                ActualReturnDate = now.AddDays(-2).AddHours(1),
                PausedMinutes = 1440,
                RepairCost = 850_000m,
                Note = "Bảo hành cụm nguồn dự phòng số 2"
            },
            new TicketVendorLog
            {
                MaintenanceTicketId = waitingVendorTicket.Id,
                VendorId = vendor.Id,
                DepartmentIdAtDispatch = itDept.Id,
                MaintenanceDepartmentIdAtDispatch = itDept.Id,
                DispatchedAt = now.AddHours(-5),
                EstimatedReturnDate = now.Date.AddDays(1),
                ActualReturnDate = null,
                PausedMinutes = 0,
                RepairCost = null,
                Note = "Chờ kỹ thuật viên hãng Daikin mang bo cảm biến tới thay"
            });

        await context.SaveChangesAsync();
    }
}
