using System.Diagnostics;
using InternalMaintenance.Assets.Infrastructure;
using InternalMaintenance.Identity.Infrastructure;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Infrastructure;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit.Abstractions;

namespace Api.IntegrationTests;

/// <summary>
/// Một container SQL Server dùng chung cho class (khởi động container là phần đắt nhất),
/// nhưng mỗi test nhận một DATABASE riêng nên việc DROP INDEX ở test đối chứng không thể lan sang test khác.
/// </summary>
public sealed class ConcurrencySqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DbContextOptions<AppDbContext> CreateOptions(string databaseName)
    {
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;
        return new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
    }
}

public sealed class EquipmentActiveTicketConcurrencyTests(
    ConcurrencySqlServerFixture fixture,
    ITestOutputHelper output) : IClassFixture<ConcurrencySqlServerFixture>
{
    private const int ParallelRequests = 50;
    private const int Rounds = 50;
    private const string IndexName = "IX_MaintenanceTickets_EquipmentId_Active";
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(60);

    // (Chính) Không cổng + có index: hành vi production thật, request tự do đua nhau.
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Main_ungated_parallel_creates_leave_exactly_one_active_ticket()
    {
        var options = fixture.CreateOptions("ConcurrencyMain");
        await CreateSchemaAsync(options);
        try
        {
            Assert.Equal(1, await CountIndexAsync(options)); // index phải tồn tại và là filtered unique.

            var summary = await RunScenarioAsync("MAIN (no gate, index ON)", options, gated: false, round =>
            {
                if (round.Exceptions.Count > 0) return $"unexpected exceptions: {string.Join(" | ", round.Exceptions.Take(3))}";
                if (round.Successes != 1) return $"successes={round.Successes}, expected 1";
                var rejected = round.Errors.Where(e => e.Key is not (CreateMaintenanceTicketError.DuplicateActiveTicket
                    or CreateMaintenanceTicketError.DuplicateRace)).Sum(e => e.Value);
                if (rejected > 0) return $"{rejected} rejections with a non-duplicate error";
                if (round.Errors.Values.Sum() != ParallelRequests - 1) return "rejection count != 49";
                if (round.ActiveInDb != 1) return $"active tickets in DB={round.ActiveInDb}, expected 1";
                return null;
            });

            Assert.Equal(0, summary.InvariantViolations);
            Assert.Equal(0, summary.ExpectationFailures);
        }
        finally
        {
            await DropDatabaseAsync(options);
        }
    }

    // (A) Có cổng + có index: ép cả 50 request cùng vượt qua domain check => index là chốt duy nhất.
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task A_gated_with_index_yields_one_success_and_49_duplicate_race()
    {
        var options = fixture.CreateOptions("ConcurrencyGatedWithIndex");
        await CreateSchemaAsync(options);
        try
        {
            Assert.Equal(1, await CountIndexAsync(options));

            var summary = await RunScenarioAsync("A (gate, index ON)", options, gated: true, round =>
            {
                if (round.Exceptions.Count > 0) return $"unexpected exceptions: {string.Join(" | ", round.Exceptions.Take(3))}";
                if (round.Successes != 1) return $"successes={round.Successes}, expected 1";
                round.Errors.TryGetValue(CreateMaintenanceTicketError.DuplicateRace, out var races);
                if (races != ParallelRequests - 1 || round.Errors.Count != 1)
                    return $"expected exactly 49 DuplicateRace, got {Describe(round.Errors)}";
                if (round.ActiveInDb != 1) return $"active tickets in DB={round.ActiveInDb}, expected 1";
                return null;
            });

            Assert.Equal(0, summary.InvariantViolations);
            Assert.Equal(0, summary.ExpectationFailures);
        }
        finally
        {
            await DropDatabaseAsync(options);
        }
    }

    // (B) Đối chứng: có cổng + KHÔNG index, trên database riêng => phải sinh trùng, chứng minh test bắt được lỗi.
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task B_gated_without_index_creates_duplicate_active_tickets()
    {
        var options = fixture.CreateOptions("ConcurrencyGatedNoIndex");
        await CreateSchemaAsync(options);
        try
        {
            // Chỉ DROP trên DB riêng của test này; assert trạng thái index qua sys.indexes trước và sau.
            Assert.Equal(1, await CountIndexAsync(options));
            await using (var db = new AppDbContext(options))
                await db.Database.ExecuteSqlRawAsync($"DROP INDEX [{IndexName}] ON [MaintenanceTickets]");
            Assert.Equal(0, await CountIndexAsync(options));

            var summary = await RunScenarioAsync("B (gate, index DROPPED)", options, gated: true, round =>
            {
                if (round.Exceptions.Count > 0) return $"unexpected exceptions: {string.Join(" | ", round.Exceptions.Take(3))}";
                return round.Successes > 1 && round.ActiveInDb > 1
                    ? null
                    : $"expected duplicates, got successes={round.Successes}, active in DB={round.ActiveInDb}";
            });

            // Mong đợi: mọi vòng đều sinh trùng (success > 1). Ở đây "InvariantViolations" là bằng chứng test bắt được lỗi.
            Assert.Equal(0, summary.ExpectationFailures);
            Assert.Equal(Rounds, summary.InvariantViolations);
            Assert.True(summary.TotalSuccesses > Rounds);
        }
        finally
        {
            await DropDatabaseAsync(options);
        }
    }

    // Lỗi DbUpdateException KHÔNG do index thiết bị (ở đây: trùng TicketCode) không được báo là DuplicateRace.
    // Thiết bị B vẫn có ticket đang chiếm (T0) để logic cũ "re-check occupying => false" bị đánh lừa.
    [Fact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Non_equipment_index_failure_is_not_reported_as_duplicate_race()
    {
        var options = fixture.CreateOptions("ConcurrencyNonEquipmentFailure");
        await CreateSchemaAsync(options);
        try
        {
            var (equipmentA, userA) = await SeedAsync(options, 1);
            var (equipmentB, userB) = await SeedAsync(options, 2);

            await using (var db = new AppDbContext(options))
            {
                db.MaintenanceTickets.AddRange(
                    MaintenanceTicket.Create("CODE-B-ACTIVE", "T0", "active on B", equipmentB, userB,
                        TicketPriorities.Medium, DateTime.UtcNow),
                    MaintenanceTicket.Create("DUP-CODE", "T1", "active on A", equipmentA, userA,
                        TicketPriorities.Medium, DateTime.UtcNow));
                await db.SaveChangesAsync();
            }

            // T2: cùng TicketCode với T1 nhưng đã Cancelled nên nằm ngoài filtered index => chỉ vi phạm unique TicketCode.
            var duplicateCode = MaintenanceTicket.Create("DUP-CODE", "T2", "dup code on B", equipmentB, userB,
                TicketPriorities.Medium, DateTime.UtcNow);
            duplicateCode.Cancel("test: avoid equipment index");

            await using var storeDb = new AppDbContext(options);
            var reader = new EquipmentTicketContextReader(storeDb);
            var store = new EfTicketCreationStore(storeDb, new IdentityUserDirectory(storeDb), reader, new VendorDirectory(storeDb));

            // Không được trả false (= DuplicateRace); phải nổi lên thành exception.
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
                store.TryAddAndSaveAsync(duplicateCode, CancellationToken.None));

            var sql = Assert.IsType<SqlException>(ex.GetBaseException());
            Assert.Equal(2601, sql.Number);
            Assert.Contains("IX_MaintenanceTickets_TicketCode", sql.Message);
            Assert.DoesNotContain(IndexName, sql.Message);

            await using var verifyDb = new AppDbContext(options);
            Assert.Equal(2, await verifyDb.MaintenanceTickets.CountAsync());
            Report("=== NON-EQUIPMENT FAILURE === DbUpdateException surfaced (SqlException 2601 on TicketCode), not mapped to DuplicateRace.");
        }
        finally
        {
            await CleanupAsync(options);
            await DropDatabaseAsync(options);
        }
    }

    // ---------------------------------------------------------------- scenario runner

    private async Task<ScenarioSummary> RunScenarioAsync(
        string name,
        DbContextOptions<AppDbContext> options,
        bool gated,
        Func<RoundResult, string?> verify)
    {
        var errorTotals = new Dictionary<CreateMaintenanceTicketError, int>();
        var exceptionTotal = 0;
        var successTotal = 0;
        var invariantViolations = 0;
        var expectationFailures = 0;
        var failureNotes = new List<string>();
        var stopwatch = Stopwatch.StartNew();

        for (var round = 1; round <= Rounds; round++)
        {
            var result = await RunRoundAsync(options, round, gated);
            successTotal += result.Successes;
            exceptionTotal += result.Exceptions.Count;
            foreach (var (error, count) in result.Errors)
                errorTotals[error] = errorTotals.GetValueOrDefault(error) + count;

            if (result.ActiveInDb > 1) invariantViolations++;
            var problem = verify(result);
            if (problem is not null)
            {
                expectationFailures++;
                if (failureNotes.Count < 5) failureNotes.Add($"round {round}: {problem}");
            }
        }

        stopwatch.Stop();
        Report($"=== {name} ===");
        Report($"Rounds                 : {Rounds} (x {ParallelRequests} parallel requests, same equipment)");
        Report($"Invariant violations   : {invariantViolations} (rounds with >1 active ticket in DB)");
        Report($"Expectation failures   : {expectationFailures}");
        Report($"Outcome totals         : Success={successTotal}, " +
               $"DuplicateActiveTicket={errorTotals.GetValueOrDefault(CreateMaintenanceTicketError.DuplicateActiveTicket)}, " +
               $"DuplicateRace={errorTotals.GetValueOrDefault(CreateMaintenanceTicketError.DuplicateRace)}, " +
               $"OtherErrors={errorTotals.Where(e => e.Key is not (CreateMaintenanceTicketError.DuplicateActiveTicket or CreateMaintenanceTicketError.DuplicateRace)).Sum(e => e.Value)}, " +
               $"UnexpectedExceptions={exceptionTotal}");
        Report($"Elapsed                : {stopwatch.Elapsed.TotalSeconds:F2}s");
        foreach (var note in failureNotes) Report($"  ! {note}");

        return new ScenarioSummary(invariantViolations, expectationFailures, successTotal);
    }

    private async Task<RoundResult> RunRoundAsync(DbContextOptions<AppDbContext> options, int round, bool gated)
    {
        var (equipmentId, userId) = await SeedAsync(options, round);
        try
        {
            var gate = gated ? new ConcurrencyGate(ParallelRequests, GateTimeout) : null;
            var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Task.Run + startSignal: tất cả task đã sẵn sàng rồi mới cùng được thả, tránh request đầu chạy xong trước khi request cuối bắt đầu.
            var tasks = Enumerable.Range(0, ParallelRequests).Select(index => Task.Run(async () =>
            {
                await startSignal.Task;
                try
                {
                    await using var db = new AppDbContext(options); // DbContext riêng cho từng task.
                    var reader = new EquipmentTicketContextReader(db);
                    ITicketCreationStore store = new EfTicketCreationStore(
                        db, new IdentityUserDirectory(db), reader, new VendorDirectory(db));
                    if (gate is not null) store = new GatedStore(store, gate, index);

                    var handler = new CreateMaintenanceTicketHandler(reader, store, new TicketCodeGenerator());
                    var outcome = await handler.Handle(
                        new CreateMaintenanceTicketCommand(
                            $"Race {round}-{index}", "Concurrent create", equipmentId, null, userId),
                        CancellationToken.None);

                    if (gate is not null && !gate.HasArrived(index))
                        gate.Abort(index, $"handler returned {outcome.Error}: {outcome.Message}");
                    return new TaskResult(outcome, null);
                }
                catch (Exception ex)
                {
                    if (gate is not null && !gate.HasArrived(index))
                        gate.Abort(index, $"{ex.GetType().Name}: {ex.Message}");
                    return new TaskResult(null, ex);
                }
            })).ToArray();

            startSignal.SetResult();
            var results = await Task.WhenAll(tasks);

            var errors = new Dictionary<CreateMaintenanceTicketError, int>();
            foreach (var outcome in results.Where(r => r.Outcome is { IsSuccess: false }).Select(r => r.Outcome!))
                errors[outcome.Error] = errors.GetValueOrDefault(outcome.Error) + 1;

            await using var verifyDb = new AppDbContext(options);
            var active = await verifyDb.MaintenanceTickets.CountAsync(ticket =>
                ticket.EquipmentId == equipmentId &&
                TicketWorkflowRules.EquipmentOccupyingStatuses.Contains(ticket.Status));

            return new RoundResult(
                results.Count(r => r.Outcome is { IsSuccess: true }),
                errors,
                results.Where(r => r.Exception is not null)
                    .Select(r => $"{r.Exception!.GetType().Name}: {r.Exception.Message}").ToList(),
                active);
        }
        finally
        {
            await CleanupAsync(options); // Dọn sau MỖI vòng, kể cả khi vòng lỗi.
        }
    }

    // ---------------------------------------------------------------- data helpers

    private static async Task CreateSchemaAsync(DbContextOptions<AppDbContext> options)
    {
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync(); // tạo database + schema từ model hiện tại (gồm filtered index).
    }

    private static async Task DropDatabaseAsync(DbContextOptions<AppDbContext> options)
    {
        await using var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }

    private static async Task<(int EquipmentId, int UserId)> SeedAsync(DbContextOptions<AppDbContext> options, int round)
    {
        await using var db = new AppDbContext(options);
        var department = new Department { Name = $"Race dept {round}" };
        var role = new Role { Name = $"Race role {round}" };
        db.AddRange(department, role);
        await db.SaveChangesAsync();

        var user = new User
        {
            FullName = $"Race user {round}",
            Email = $"race-{round}@example.test",
            PasswordHash = "test-only",
            RoleId = role.Id,
            DepartmentId = department.Id,
            IsActive = true
        };
        var equipment = new Equipment
        {
            Code = $"EQ-RACE-{round}",
            Name = $"Race equipment {round}",
            DepartmentId = department.Id,
            MaintenanceDepartmentId = department.Id,
            Status = "Active"
        };
        db.AddRange(user, equipment);
        await db.SaveChangesAsync();
        return (equipment.Id, user.Id);
    }

    // Mỗi test có database riêng và các vòng chạy tuần tự, nên xoá toàn bộ theo thứ tự FK là an toàn.
    private static async Task CleanupAsync(DbContextOptions<AppDbContext> options)
    {
        await using var db = new AppDbContext(options);
        await db.TicketStatusHistories.ExecuteDeleteAsync();
        await db.TicketComments.ExecuteDeleteAsync();
        await db.TicketVendorLogs.ExecuteDeleteAsync();
        await db.MaintenanceTickets.ExecuteDeleteAsync();
        await db.Equipment.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
        await db.Roles.ExecuteDeleteAsync();
        await db.Departments.ExecuteDeleteAsync();
    }

    private static async Task<int> CountIndexAsync(DbContextOptions<AppDbContext> options)
    {
        await using var db = new AppDbContext(options);
        return await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.indexes " +
            "WHERE object_id = OBJECT_ID(N'[MaintenanceTickets]') " +
            $"AND name = N'{IndexName}' AND is_unique = 1 AND has_filter = 1").SingleAsync();
    }

    private void Report(string line)
    {
        output.WriteLine(line);
        Console.WriteLine(line); // xUnit v2 không hiển thị Console mặc định; ITestOutputHelper là đường chính.
    }

    private static string Describe(Dictionary<CreateMaintenanceTicketError, int> errors) =>
        errors.Count == 0 ? "none" : string.Join(", ", errors.Select(e => $"{e.Key}={e.Value}"));

    // ---------------------------------------------------------------- test-only types

    private sealed record TaskResult(CreateMaintenanceTicketOutcome? Outcome, Exception? Exception);

    private sealed record RoundResult(
        int Successes,
        Dictionary<CreateMaintenanceTicketError, int> Errors,
        List<string> Exceptions,
        int ActiveInDb);

    private sealed record ScenarioSummary(int InvariantViolations, int ExpectationFailures, int TotalSuccesses);

    /// <summary>
    /// Cổng async: mỗi task báo "đã qua domain check" (Interlocked), task cuối mở cổng bằng TaskCompletionSource.
    /// Có timeout; nếu một task chết trước khi tới cổng thì Abort() làm mọi task đang chờ nhận lỗi rõ nguyên nhân
    /// thay vì treo tới timeout.
    /// </summary>
    private sealed class ConcurrencyGate(int parties, TimeSpan timeout)
    {
        private readonly TaskCompletionSource _open = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly int[] _arrived = new int[parties];
        private int _arrivedCount;

        public bool HasArrived(int index) => Volatile.Read(ref _arrived[index]) == 1;

        public async Task ArriveAndWaitAsync(int index)
        {
            if (Interlocked.Exchange(ref _arrived[index], 1) == 0 &&
                Interlocked.Increment(ref _arrivedCount) == parties)
                _open.TrySetResult();

            try
            {
                await _open.Task.WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    $"Gate timeout after {timeout.TotalSeconds:F0}s: only {Volatile.Read(ref _arrivedCount)}/{parties} tasks reached the gate.");
            }
        }

        public void Abort(int index, string reason) =>
            _open.TrySetException(new InvalidOperationException(
                $"Task #{index} ended before reaching the gate ({Volatile.Read(ref _arrivedCount)}/{parties} arrived): {reason}"));
    }

    /// <summary>
    /// Decorator chỉ để đồng bộ: gọi store thật, rồi đợi mọi request cùng hoàn tất domain check trước khi đi tiếp tới INSERT.
    /// Không đổi kết quả của store.
    /// </summary>
    private sealed class GatedStore(ITicketCreationStore inner, ConcurrencyGate gate, int index) : ITicketCreationStore
    {
        public async Task<bool> HasEquipmentOccupyingTicketAsync(int equipmentId, CancellationToken cancellationToken)
        {
            var occupied = await inner.HasEquipmentOccupyingTicketAsync(equipmentId, cancellationToken);
            await gate.ArriveAndWaitAsync(index);
            return occupied;
        }

        public Task<bool> TryAddAndSaveAsync(MaintenanceTicket ticket, CancellationToken cancellationToken) =>
            inner.TryAddAndSaveAsync(ticket, cancellationToken);

        public Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken) =>
            inner.GetResponseAsync(ticketId, cancellationToken);
    }
}

