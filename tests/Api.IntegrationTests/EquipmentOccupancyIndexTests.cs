using InternalMaintenance.Api.Extensions;
using InternalMaintenance.TicketAttachments.Domain;
using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.Assets.Domain;
using InternalMaintenance.Api.Modules.Tickets;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Assets.Infrastructure;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Infrastructure;
using InternalMaintenance.Maintenance.Infrastructure.Reporting;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Infrastructure;
using InternalMaintenance.Organization.Domain;
using InternalMaintenance.Persistence;
using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure;
using InternalMaintenance.TicketAttachments.Infrastructure;
using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Infrastructure;
using InternalMaintenance.BuildingBlocks.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;

namespace Api.IntegrationTests;

public sealed class EquipmentOccupancyIndexTests
{
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Sla_report_to_date_includes_tickets_created_later_on_that_day()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(sqlServer.GetConnectionString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var ticketId = await CreateOneHourOverdueTicketAsync(db, "REPORTDATE");

        var ticket = await db.MaintenanceTickets.SingleAsync(item => item.Id == ticketId);
        ticket.CreatedAt = DateTime.UtcNow.Date.AddHours(12);
        await db.SaveChangesAsync();

        var reportService = new SlaComplianceReportService(
            new EfSlaComplianceReadStore(db),
            new TestReportActorContext(UserRoles.Admin, null));
        var report = await reportService.GetAsync(new ReportFilterQuery
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date
        });

        // ToDate đại diện cho cả ngày đã chọn; ticket lúc 12:00 phải được tính dù filter bind thành 00:00.
        Assert.Equal(1, report.TotalTickets);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Maintenance_cost_report_aggregates_duplicate_dispatches_without_double_counting_ticket()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(sqlServer.GetConnectionString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var department = new Department { Name = "Reporting cost test" };
        var role = new Role { Name = "Reporting cost test" };
        db.AddRange(department, role);
        await db.SaveChangesAsync();

        var user = new User
        {
            FullName = "Reporting cost test user",
            Email = "reporting-cost@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            DepartmentId = department.Id,
            IsActive = true
        };
        var equipment = new Equipment
        {
            Code = "EQ-REPORT-COST",
            Name = "Reporting cost equipment",
            DepartmentId = department.Id
        };
        db.AddRange(user, equipment);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var ticket = MaintenanceTicket.Create(
            "MT-REPORT-COST", "Vendor repair", "Exercise SQL cost aggregation",
            equipment.Id, user.Id, TicketPriorities.Medium, now.AddDays(-1));
        db.MaintenanceTickets.Add(ticket);
        await db.SaveChangesAsync();

        db.TicketVendorLogs.AddRange(
            new TicketVendorLog
            {
                MaintenanceTicketId = ticket.Id,
                VendorId = 9001,
                DepartmentIdAtDispatch = department.Id,
                DispatchedAt = now.AddHours(-2),
                EstimatedReturnDate = now.AddDays(1),
                RepairCost = 100m
            },
            new TicketVendorLog
            {
                MaintenanceTicketId = ticket.Id,
                VendorId = 9001,
                DepartmentIdAtDispatch = department.Id,
                DispatchedAt = now.AddHours(-1),
                EstimatedReturnDate = now.AddDays(1),
                RepairCost = 50m
            });
        await db.SaveChangesAsync();

        var reportService = new MaintenanceCostReportService(
            new EfMaintenanceCostReadStore(db),
            new TestReportActorContext(UserRoles.Admin, null),
            new TestVendorDirectory());
        var report = await reportService.GetAsync(new ReportFilterQuery
        {
            FromDate = now.Date,
            ToDate = now.Date
        });

        // Hai log cùng ticket phải cộng đủ chi phí/lượt gửi nhưng chỉ tính một ticket ở trung bình, tháng và top thiết bị.
        Assert.Equal(150m, report.TotalMaintenanceCost);
        Assert.Equal(2, report.TotalVendorDispatches);
        Assert.Equal(150m, report.AvgCostPerTicket);
        var currentMonth = report.MonthlyCosts.Single(item => item.Month == now.ToString("MM/yyyy"));
        Assert.Equal(150m, currentMonth.TotalCost);
        Assert.Equal(1, currentMonth.TicketCount);
        Assert.Equal(1, Assert.Single(report.TopCostlyEquipment).MaintenanceCount);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Failed_sla_email_remains_in_outbox_and_is_retried_until_success()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var email = new FailOnceSlaEmailService();
        await using var provider = CreateSlaWorkerProvider(sqlServer.GetConnectionString(), email);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            await CreateOneHourOverdueTicketAsync(db, "RETRY");
        }

        using (var firstWorker = CreateSlaWorker(provider))
        {
            await firstWorker.StartAsync(CancellationToken.None);
            await email.FirstAttempt.Task.WaitAsync(TimeSpan.FromSeconds(30));
            // Fake email báo thời điểm bắt đầu attempt; đợi worker ghi lỗi/retry vào SQL trước khi dừng nó.
            await WaitForOutboxFailureAsync(provider);
            await firstWorker.StopAsync(CancellationToken.None);
        }

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var message = await db.SlaNotificationOutboxMessages.SingleAsync();

            // Lần gửi lỗi không xóa message: attempt/error được lưu và lần gửi sau có thời điểm retry.
            Assert.Null(message.ProcessedAt);
            Assert.Equal(1, message.AttemptCount);
            Assert.Contains("simulated email failure", message.LastError);
            message.NextAttemptAt = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        using (var retryWorker = CreateSlaWorker(provider))
        {
            await retryWorker.StartAsync(CancellationToken.None);
            await email.SecondAttempt.Task.WaitAsync(TimeSpan.FromSeconds(30));
            // Đợi worker commit ProcessedAt sau khi adapter trả thành công để kiểm thử đúng trạng thái bền vững.
            await WaitForOutboxProcessedAsync(provider);
            await retryWorker.StopAsync(CancellationToken.None);
        }

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var message = await db.SlaNotificationOutboxMessages.SingleAsync();
            Assert.NotNull(message.ProcessedAt);
            Assert.Equal(2, message.AttemptCount);
            Assert.Null(message.LastError);
            // Email đầu tiên bị lỗi và lần retry gửi thành công, nên adapter phải được gọi đúng hai lần.
            Assert.Equal(2, email.BreachCallCount);
        }
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Two_sla_workers_queue_and_send_one_breach_notification()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var email = new SlowSlaEmailService();
        await using var provider = CreateSlaWorkerProvider(sqlServer.GetConnectionString(), email);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            await CreateOneHourOverdueTicketAsync(db, "CONCURRENT");
        }

        using var firstWorker = CreateSlaWorker(provider);
        using var secondWorker = CreateSlaWorker(provider);
        await Task.WhenAll(
            firstWorker.StartAsync(CancellationToken.None),
            secondWorker.StartAsync(CancellationToken.None));
        await email.BreachSent.Task.WaitAsync(TimeSpan.FromSeconds(30));
        // Tín hiệu fake chỉ xác nhận API email đã được gọi; đợi lease được commit hoàn tất trước khi cancel worker.
        await WaitForOutboxProcessedAsync(provider);
        await Task.WhenAll(
            firstWorker.StopAsync(CancellationToken.None),
            secondWorker.StopAsync(CancellationToken.None));

        using var verifyScope = provider.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await verifyDb.SlaNotificationOutboxMessages.CountAsync());
        Assert.NotNull((await verifyDb.SlaNotificationOutboxMessages.SingleAsync()).ProcessedAt);
        Assert.Equal(1, email.BreachCallCount);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Latest_migrations_apply_and_vendor_fk_migration_rolls_back_cleanly()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(sqlServer.GetConnectionString(), sql =>
                sql.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.GetName().Name))
            .Options;
        await using var db = new AppDbContext(options);
        var migrator = db.Database.GetService<IMigrator>();

        // Dùng migration thật trên database rỗng để bắt lỗi khác biệt giữa snapshot và schema.
        await migrator.MigrateAsync();
        Assert.Equal(1, await ScalarIntAsync(db,
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE [name] = N'SlaNotificationOutboxMessages'"));
        Assert.Equal(0, await ScalarIntAsync(db,
            "SELECT COUNT(*) AS [Value] FROM sys.foreign_keys WHERE [name] = N'FK_TicketVendorLogs_Vendors_VendorId'"));

        // Rollback một migration phải khôi phục đúng FK Vendor; reapply phải bỏ FK lại mà không tạo bảng lỗi.
        await migrator.MigrateAsync("20261023153019_AddSlaNotificationOutbox");
        Assert.Equal(1, await ScalarIntAsync(db,
            "SELECT COUNT(*) AS [Value] FROM sys.foreign_keys WHERE [name] = N'FK_TicketVendorLogs_Vendors_VendorId'"));

        await migrator.MigrateAsync();
        Assert.Equal(0, await ScalarIntAsync(db,
            "SELECT COUNT(*) AS [Value] FROM sys.foreign_keys WHERE [name] = N'FK_TicketVendorLogs_Vendors_VendorId'"));
        Assert.Equal(1, await ScalarIntAsync(db,
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE [name] = N'SlaNotificationOutboxMessages'"));
    }

    private static async Task<int> ScalarIntAsync(AppDbContext db, string sql) =>
        await db.Database.SqlQueryRaw<int>(sql).SingleAsync();

    private static ServiceProvider CreateSlaWorkerProvider(string connectionString, IEmailService emailService)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddLogging();
        services.AddSingleton<IEmailService>(emailService);
        return services.BuildServiceProvider();
    }

    private static SlaMonitorWorker CreateSlaWorker(IServiceProvider provider) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<SlaMonitorWorker>>());

    private static async Task WaitForOutboxFailureAsync(IServiceProvider provider)
    {
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < timeout)
        {
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (await db.SlaNotificationOutboxMessages.AnyAsync(message => message.LastError != null))
                return;

            await Task.Delay(100);
        }

        throw new TimeoutException("SLA outbox did not persist the failed email attempt.");
    }

    private static async Task WaitForOutboxProcessedAsync(IServiceProvider provider)
    {
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < timeout)
        {
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (await db.SlaNotificationOutboxMessages.AnyAsync(message => message.ProcessedAt != null))
                return;

            await Task.Delay(100);
        }

        throw new TimeoutException("SLA outbox did not persist successful email delivery.");
    }

    private static async Task<int> CreateOneHourOverdueTicketAsync(AppDbContext db, string suffix)
    {
        var department = new Department { Name = $"SLA {suffix}" };
        var role = new Role { Name = $"SLA {suffix}" };
        db.AddRange(department, role);
        await db.SaveChangesAsync();

        var user = new User
        {
            FullName = $"SLA {suffix} Technician",
            Email = $"sla-{suffix.ToLowerInvariant()}@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            DepartmentId = department.Id,
            IsActive = true
        };
        var equipment = new Equipment
        {
            Code = $"EQ-SLA-{suffix}",
            Name = $"SLA {suffix} equipment",
            DepartmentId = department.Id,
            MaintenanceDepartmentId = department.Id
        };
        db.AddRange(user, equipment);
        await db.SaveChangesAsync();

        // Priority High có SLA 8 giờ; ticket tạo 9 giờ trước vừa quá hạn khoảng 1 giờ,
        // nên worker chỉ phải phát cảnh báo breach, chưa đến mốc escalation hai giờ.
        var createdAt = DateTime.UtcNow.AddHours(-9);
        var ticket = MaintenanceTicket.Create(
            $"MT-SLA-{suffix}", $"SLA {suffix}", "Exercise durable outbox delivery",
            equipment.Id, user.Id, TicketPriorities.High, createdAt);
        ticket.Assign(user.Id, null);
        ticket.Start(createdAt.AddMinutes(1));
        db.MaintenanceTickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket.Id;
    }

    private abstract class SlaEmailServiceStub : IEmailService
    {
        public abstract Task SendBreachedAlertAsync(
            MaintenanceTicket ticket,
            CancellationToken cancellationToken = default,
            string? idempotencyKey = null);

        public Task SendNearBreachAlertAsync(
            MaintenanceTicket ticket,
            CancellationToken cancellationToken = default,
            string? idempotencyKey = null) =>
            Task.CompletedTask;

        public Task SendEscalationAlertAsync(
            MaintenanceTicket ticket,
            CancellationToken cancellationToken = default,
            string? idempotencyKey = null) =>
            Task.CompletedTask;

        public Task SendSlaResultAsync(
            MaintenanceTicket ticket, bool isMet, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FailOnceSlaEmailService : SlaEmailServiceStub
    {
        private int _breachCallCount;
        public int BreachCallCount => Volatile.Read(ref _breachCallCount);
        public TaskCompletionSource FirstAttempt { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondAttempt { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task SendBreachedAlertAsync(
            MaintenanceTicket ticket,
            CancellationToken cancellationToken = default,
            string? idempotencyKey = null)
        {
            if (Interlocked.Increment(ref _breachCallCount) == 1)
            {
                FirstAttempt.TrySetResult();
                return Task.FromException(new InvalidOperationException("simulated email failure"));
            }

            SecondAttempt.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class SlowSlaEmailService : SlaEmailServiceStub
    {
        private int _breachCallCount;
        public int BreachCallCount => Volatile.Read(ref _breachCallCount);
        public TaskCompletionSource BreachSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task SendBreachedAlertAsync(
            MaintenanceTicket ticket,
            CancellationToken cancellationToken = default,
            string? idempotencyKey = null)
        {
            Interlocked.Increment(ref _breachCallCount);
            await Task.Delay(300, cancellationToken); // Giữ lease trong lúc instance còn lại tranh claim.
            BreachSent.TrySetResult();
        }
    }

    private sealed class TestReportActorContext(string role, int? departmentId) : IReportActorContext
    {
        public string Role { get; } = role;
        public int? DepartmentId { get; } = departmentId;
    }

    private sealed class TestVendorDirectory : IVendorDirectory
    {
        public Task<bool> IsActiveAsync(int vendorId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<int, string>> FindNamesAsync(
            IReadOnlyCollection<int> vendorIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(
                vendorIds.ToDictionary(vendorId => vendorId, vendorId => $"Vendor {vendorId}"));
    }

    private sealed class TestTicketAttachmentActorContext(int userId, string role, int? departmentId)
        : ITicketAttachmentActorContext
    {
        public int UserId { get; } = userId;
        public string Role { get; } = role;
        public int? DepartmentId { get; } = departmentId;
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Attachment_cleanup_retries_failed_storage_delete_and_skips_completed_record()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(sqlServer.GetConnectionString()));
        services.AddLogging();
        var storage = new FailOnceAttachmentStorage();
        services.AddSingleton<IAttachmentStorageService>(storage);
        services.AddScoped<AttachmentStorageCleanupProcessor>();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var department = new Department { Name = "Attachment Cleanup" };
        var role = new Role { Name = "Staff" };
        db.AddRange(department, role);
        await db.SaveChangesAsync();

        var user = new User
        {
            FullName = "Attachment Owner",
            Email = "attachment-cleanup@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        var equipment = new Equipment
        {
            Code = "EQ-ATTACH-CLEANUP",
            Name = "Attachment cleanup test equipment",
            DepartmentId = department.Id
        };
        db.AddRange(user, equipment);
        await db.SaveChangesAsync();

        var ticket = MaintenanceTicket.Create(
            "MT-ATTACH-CLEANUP", "Attachment cleanup", "Retry storage deletion",
            equipment.Id, user.Id, "Medium", DateTime.UtcNow);
        db.MaintenanceTickets.Add(ticket);
        await db.SaveChangesAsync();

        var attachment = new TicketAttachment
        {
            MaintenanceTicketId = ticket.Id,
            UploadedByUserId = user.Id,
            OriginalFileName = "report.pdf",
            StoredFileName = "stored-report.pdf",
            ContentType = "application/pdf",
            FileSize = 128,
            StorageKey = "tickets/MT-ATTACH-CLEANUP/attachments/stored-report.pdf",
            FileType = "document",
            IsDeleted = false
        };
        db.TicketAttachments.Add(attachment);
        await db.SaveChangesAsync();

        var identityDirectory = new IdentityUserDirectory(db);
        var equipmentReader = new EquipmentTicketContextReader(db);
        var attachmentService = new TicketAttachmentsService(
            db,
            new TestTicketAttachmentActorContext(user.Id, UserRoles.Admin, null),
            storage,
            identityDirectory,
            new MaintenanceTicketLookup(db),
            new MaintenanceTicketAccessChecker(equipmentReader, identityDirectory),
            Options.Create(new R2AttachmentStorageOptions()));

        // API lưu ý định xóa vào SQL trước khi gọi storage. Nếu storage lỗi tạm thời,
        // worker vẫn tìm thấy record IsDeleted nhưng chưa có StorageDeletedAt để retry.
        var deleteResult = await attachmentService.DeleteAsync(ticket.Id, attachment.Id);
        Assert.True(deleteResult.IsSuccess);
        Assert.Equal(202, deleteResult.StatusCode);
        db.ChangeTracker.Clear();
        var pendingDelete = await db.TicketAttachments.SingleAsync();
        Assert.True(pendingDelete.IsDeleted);
        Assert.Null(pendingDelete.StorageDeletedAt);

        var processor = scope.ServiceProvider.GetRequiredService<AttachmentStorageCleanupProcessor>();
        // Hai processor mô phỏng hai API instance. Cả hai có thể đọc cùng ứng viên,
        // nhưng chỉ instance claim lease bằng UPDATE có điều kiện mới được gọi storage.
        using var secondScope = provider.CreateScope();
        var secondProcessor = secondScope.ServiceProvider.GetRequiredService<AttachmentStorageCleanupProcessor>();
        await Task.WhenAll(processor.ProcessBatchAsync(), secondProcessor.ProcessBatchAsync());
        db.ChangeTracker.Clear();
        Assert.NotNull((await db.TicketAttachments.SingleAsync()).StorageDeletedAt);

        await processor.ProcessBatchAsync();
        Assert.Equal(2, storage.DeleteCallCount);
        // Lần 1 thất bại; trong hai worker đồng thời chỉ một lần DELETE nữa được phát ra.
        Assert.All(storage.RequestedKeys, key => Assert.Equal(attachment.StorageKey, key));
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Expired_unconfirmed_upload_intent_is_retried_until_orphan_object_is_deleted()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(sqlServer.GetConnectionString()));
        services.AddLogging();
        var storage = new FailOnceAttachmentStorage();
        services.AddSingleton<IAttachmentStorageService>(storage);
        services.AddScoped<AttachmentStorageCleanupProcessor>();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var department = new Department { Name = "Orphan Upload Cleanup" };
        var role = new Role { Name = "Staff" };
        db.AddRange(department, role);
        await db.SaveChangesAsync();

        var user = new User
        {
            FullName = "Upload Owner",
            Email = "upload-owner@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        var equipment = new Equipment
        {
            Code = "EQ-UPLOAD-ORPHAN",
            Name = "Orphan upload test equipment",
            DepartmentId = department.Id
        };
        db.AddRange(user, equipment);
        await db.SaveChangesAsync();

        var ticket = MaintenanceTicket.Create(
            "MT-UPLOAD-ORPHAN", "Upload cleanup", "Upload completed but confirm was lost",
            equipment.Id, user.Id, "Medium", DateTime.UtcNow);
        db.MaintenanceTickets.Add(ticket);
        await db.SaveChangesAsync();

        var identityDirectory = new IdentityUserDirectory(db);
        var equipmentReader = new EquipmentTicketContextReader(db);
        var attachmentService = new TicketAttachmentsService(
            db,
            new TestTicketAttachmentActorContext(user.Id, UserRoles.Admin, null),
            storage,
            identityDirectory,
            new MaintenanceTicketLookup(db),
            new MaintenanceTicketAccessChecker(equipmentReader, identityDirectory),
            Options.Create(new R2AttachmentStorageOptions()));

        // Presign phải tạo intent SQL trước khi URL được trả về. Khi confirm thành công, intent được
        // chuyển thành attachment trong transaction; worker từ đó không còn xem file hợp lệ là orphan.
        var confirmedPresign = await attachmentService.CreatePresignAsync(ticket.Id, new PresignAttachmentRequest
        {
            FileName = "confirmed.pdf",
            ContentType = "application/pdf",
            FileSize = 128
        });
        Assert.True(confirmedPresign.IsSuccess);
        var confirmResult = await attachmentService.ConfirmAsync(ticket.Id, new ConfirmAttachmentRequest
        {
            StorageKey = confirmedPresign.Data!.StorageKey,
            OriginalFileName = "confirmed.pdf",
            StoredFileName = confirmedPresign.Data.StoredFileName,
            ContentType = "application/pdf",
            FileSize = 128
        });
        Assert.True(confirmResult.IsSuccess);
        var confirmedAttachment = await db.TicketAttachments.SingleAsync();
        Assert.Equal(128, confirmedAttachment.FileSize);
        Assert.Empty(await db.TicketAttachmentUploadIntents.ToListAsync());

        // Lần upload thứ hai giả lập browser upload xong rồi đóng tab mà không gọi Confirm.
        // Hết hạn + thời gian retry, intent vẫn giữ StorageKey để worker tìm và xóa object mồ côi.
        var presign = await attachmentService.CreatePresignAsync(ticket.Id, new PresignAttachmentRequest
        {
            FileName = "orphan.pdf",
            ContentType = "application/pdf",
            FileSize = 128
        });
        Assert.True(presign.IsSuccess);
        var intent = await db.TicketAttachmentUploadIntents.SingleAsync();
        Assert.Equal(presign.Data!.StorageKey, intent.StorageKey);
        Assert.Null(intent.ConfirmedAt);

        // Client khai file 128 byte nhưng HEAD từ storage báo 129 byte. Chặn confirm để DB không
        // ghi metadata sai; intent được giữ lại để worker dọn object khi URL đã hết hạn.
        storage.ObjectSizeBytes = 129;
        var mismatchedConfirm = await attachmentService.ConfirmAsync(ticket.Id, new ConfirmAttachmentRequest
        {
            StorageKey = presign.Data.StorageKey,
            OriginalFileName = "orphan.pdf",
            StoredFileName = presign.Data.StoredFileName,
            ContentType = "application/pdf",
            FileSize = 128
        });
        Assert.False(mismatchedConfirm.IsSuccess);
        Assert.Equal(StatusCodes.Status400BadRequest, mismatchedConfirm.StatusCode);
        Assert.Single(await db.TicketAttachments.ToListAsync());

        // Giả lập URL đã hết hạn quá 30 phút để worker được phép dọn, không thay đổi đồng hồ của máy test.
        intent.UploadUrlExpiresAt = DateTime.UtcNow.AddMinutes(-31);
        await db.SaveChangesAsync();

        var processor = scope.ServiceProvider.GetRequiredService<AttachmentStorageCleanupProcessor>();
        await processor.ProcessBatchAsync();
        db.ChangeTracker.Clear();
        Assert.Null((await db.TicketAttachmentUploadIntents.SingleAsync()).StorageDeletedAt);

        // خطای موقت storage نباید intent را پاک کند؛ چرخه بعدی باید همان object orphan را دوباره امتحان کند.
        await processor.ProcessBatchAsync();
        db.ChangeTracker.Clear();
        Assert.NotNull((await db.TicketAttachmentUploadIntents.SingleAsync()).StorageDeletedAt);
        Assert.Equal(2, storage.DeleteCallCount);
        Assert.Equal(2, storage.RequestedKeys.Count);
        Assert.All(storage.RequestedKeys, key => Assert.Equal(presign.Data.StorageKey, key));
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Failure_on_second_save_rolls_back_user_created_by_first_save()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(sqlServer.GetConnectionString())
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var role = new Role { Name = "Staff" };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var existingUser = new User
        {
            FullName = "Existing User",
            Email = "existing-transaction@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existingUser.Id,
            Token = "duplicate-refresh-token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        await using var transaction = await db.Database.BeginTransactionAsync();

        var provisionedUser = new User
        {
            FullName = "New Google User",
            Email = "new-google-user@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        db.Users.Add(provisionedUser);
        await db.SaveChangesAsync(); // Lần lưu đầu lấy ID identity, nhưng transaction chưa commit.

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = provisionedUser.Id,
            Token = "duplicate-refresh-token", // Cố ý vi phạm unique index để giả lập lỗi lưu token.
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        await transaction.RollbackAsync();
        db.ChangeTracker.Clear();

        // Tài khoản tạo ở lần SaveChanges đầu không được tồn tại nếu lần lưu refresh token thất bại.
        Assert.False(await db.Users.AnyAsync(user => user.Email == "new-google-user@example.test"));
        Assert.Equal(1, await db.RefreshTokens.CountAsync(token => token.UserId == existingUser.Id));
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Ticket_equipment_and_history_changes_roll_back_as_one_sql_transaction()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(sqlServer.GetConnectionString())
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var department = new Department { Name = "Maintenance" };
        var role = new Role { Name = "Technician" };
        db.AddRange(department, role);
        await db.SaveChangesAsync();

        var requester = new User
        {
            FullName = "Rollback Requester",
            Email = "rollback-requester@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        var technician = new User
        {
            FullName = "Rollback Technician",
            Email = "rollback-technician@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        var equipment = new Equipment
        {
            Code = "EQ-ROLLBACK-001",
            Name = "Rollback test equipment",
            DepartmentId = department.Id,
            MaintenanceDepartmentId = department.Id,
            Status = "Active"
        };
        db.AddRange(requester, technician, equipment);
        await db.SaveChangesAsync();

        var ticket = MaintenanceTicket.Create(
            "MT-ROLLBACK-001", "Rollback test", "Verify shared transaction",
            equipment.Id, requester.Id, "Medium", DateTime.UtcNow);
        db.MaintenanceTickets.Add(ticket);
        await db.SaveChangesAsync();

        // Ticket và Equipment được sửa hợp lệ, nhưng actor giả lập không tồn tại nên History vi phạm FK.
        // EF phải rollback toàn bộ SaveChanges, không để hai module lưu trạng thái nửa chừng.
        ticket.Assign(technician.Id, null);
        ticket.Start(DateTime.UtcNow);
        equipment.Status = "UnderMaintenance";
        db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = "Pending",
            NewStatus = "InProgress",
            ChangedByUserId = int.MaxValue,
            ChangedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var persistedTicket = await db.MaintenanceTickets.SingleAsync(item => item.Id == ticket.Id);
        var persistedEquipment = await db.Equipment.SingleAsync(item => item.Id == equipment.Id);
        var historyCount = await db.TicketStatusHistories.CountAsync(
            item => item.MaintenanceTicketId == ticket.Id);

        Assert.Equal("Pending", persistedTicket.Status);
        Assert.Equal("Active", persistedEquipment.Status);
        Assert.Equal(0, historyCount);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Status_use_case_rolls_back_ticket_and_equipment_when_history_insert_fails()
    {
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(sqlServer.GetConnectionString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var department = new Department { Name = "Use case transaction" };
        var role = new Role { Name = "Technician" };
        db.AddRange(department, role);
        await db.SaveChangesAsync();

        var requester = new User
        {
            FullName = "Transaction Requester",
            Email = "transaction-requester@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        var technician = new User
        {
            FullName = "Transaction Technician",
            Email = "transaction-technician@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        var equipment = new Equipment
        {
            Code = "EQ-USECASE-ROLLBACK",
            Name = "Use case rollback equipment",
            DepartmentId = department.Id,
            MaintenanceDepartmentId = department.Id,
            Status = "Active"
        };
        db.AddRange(requester, technician, equipment);
        await db.SaveChangesAsync();

        var ticket = MaintenanceTicket.Create(
            "MT-USECASE-ROLLBACK", "Use case rollback", "Fail status history insert",
            equipment.Id, requester.Id, TicketPriorities.Medium, DateTime.UtcNow);
        ticket.Assign(technician.Id, null);
        db.MaintenanceTickets.Add(ticket);
        await db.SaveChangesAsync();

        // Cùng một scoped DbContext được đưa vào Store, Equipment gateway và directory adapters.
        // Actor Admin giả lập không tồn tại để History vi phạm FK đúng tại lần commit của use case.
        var identityDirectory = new IdentityUserDirectory(db);
        var equipmentReader = new EquipmentTicketContextReader(db);
        var vendorDirectory = new VendorDirectory(db);
        var store = new EfTicketStatusChangeStore(
            db, identityDirectory, equipmentReader, vendorDirectory);
        var handler = new ChangeTicketStatusHandler(
            store,
            new EquipmentMaintenanceGateway(db),
            equipmentReader,
            identityDirectory,
            vendorDirectory);

        var command = new ChangeTicketStatusCommand(
            ticket.Id,
            TicketStatuses.InProgress,
            ResolutionNote: null,
            CancellationReason: null,
            Note: "Integration test",
            VendorId: null,
            VendorEstimatedReturnDate: null,
            VendorNote: null,
            ActorUserId: int.MaxValue,
            ActorRole: UserRoles.Admin,
            ActorDepartmentId: null);

        await Assert.ThrowsAsync<DbUpdateException>(() => handler.Handle(command, CancellationToken.None));
        db.ChangeTracker.Clear();

        var persistedTicket = await db.MaintenanceTickets.SingleAsync(item => item.Id == ticket.Id);
        var persistedEquipment = await db.Equipment.SingleAsync(item => item.Id == equipment.Id);
        var historyCount = await db.TicketStatusHistories.CountAsync(
            item => item.MaintenanceTicketId == ticket.Id);

        // Khi một phần của lần commit thất bại, SQL không được giữ Ticket InProgress hay Equipment UnderMaintenance.
        Assert.Equal(TicketStatuses.Assigned, persistedTicket.Status);
        Assert.Equal("Active", persistedEquipment.Status);
        Assert.Equal(0, historyCount);
    }

    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Resolved_ticket_blocks_second_ticket_until_closed()
    {
        // SQL Server thật là cần thiết: EF InMemory không thực thi filtered unique index.
        await using var sqlServer = new MsSqlBuilder().Build();
        await sqlServer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(sqlServer.GetConnectionString())
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var department = new Department { Name = "Maintenance" };
        var role = new Role { Name = "Staff" };
        db.AddRange(department, role);
        await db.SaveChangesAsync();

        var user = new User
        {
            FullName = "Test Requester",
            Email = "requester@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        var technician = new User
        {
            FullName = "Test Technician",
            Email = "technician@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            IsActive = true
        };
        var equipment = new Equipment
        {
            Code = "EQ-TEST-001",
            Name = "Test printer",
            DepartmentId = department.Id
        };
        db.AddRange(user, technician, equipment);
        await db.SaveChangesAsync();

        db.MaintenanceTickets.Add(CreateTicket("MT-TEST-001", equipment.Id, user.Id, technician.Id, "Resolved"));
        await db.SaveChangesAsync();

        db.MaintenanceTickets.Add(CreateTicket("MT-TEST-002", equipment.Id, user.Id, technician.Id, "Pending"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var resolvedTicket = await db.MaintenanceTickets.SingleAsync(t => t.TicketCode == "MT-TEST-001");
        resolvedTicket.Close(DateTime.UtcNow);
        await db.SaveChangesAsync();

        db.MaintenanceTickets.Add(CreateTicket("MT-TEST-003", equipment.Id, user.Id, technician.Id, "Pending"));
        await db.SaveChangesAsync();
    }

    private static MaintenanceTicket CreateTicket(string code, int equipmentId, int userId, int technicianId, string status)
    {
        var now = DateTime.UtcNow;
        var ticket = MaintenanceTicket.Create(code, "Test maintenance ticket",
            "Created by SQL Server integration test", equipmentId, userId, "Medium", now);
        if (status == "Resolved")
        {
            ticket.Assign(technicianId, null);
            ticket.Start(now.AddMinutes(1));
            ticket.Resolve("Printer repaired", now.AddMinutes(2));
        }

        return ticket;
    }

    private sealed class FailOnceAttachmentStorage : IAttachmentStorageService
    {
        private int _deleteCallCount;
        public int DeleteCallCount => Volatile.Read(ref _deleteCallCount);
        public long ObjectSizeBytes { get; set; } = 128;
        public System.Collections.Concurrent.ConcurrentBag<string> RequestedKeys { get; } = [];

        public Task<ServiceResult<string>> CreateUploadUrlAsync(string storageKey, string contentType) =>
            Task.FromResult(ServiceResult<string>.Success("https://upload.example.test"));

        public Task<ServiceResult<string>> CreateDownloadUrlAsync(string storageKey) =>
            Task.FromResult(ServiceResult<string>.Fail(500, "Not used by this test"));

        public Task<ServiceResult<AttachmentObjectMetadata?>> GetObjectMetadataAsync(string storageKey) =>
            Task.FromResult(ServiceResult<AttachmentObjectMetadata?>.Success(
                new AttachmentObjectMetadata(ObjectSizeBytes)));

        public async Task<ServiceResult> DeleteObjectAsync(string storageKey)
        {
            RequestedKeys.Add(storageKey);
            var callNumber = Interlocked.Increment(ref _deleteCallCount);
            // Kéo dài giả lập request storage để tạo overlap giữa hai instance trong test.
            if (callNumber > 1)
                await Task.Delay(100);

            return callNumber == 1
                ? ServiceResult.Fail(503, "Temporary storage failure")
                : ServiceResult.Success();
        }
    }
}
