using InternalMaintenance.Persistence;
using InternalMaintenance.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Maintenance.Domain.Tests;

public class EquipmentOccupancyModelTests
{
    [Fact]
    public void SqlServer_filtered_unique_index_matches_equipment_occupancy_statuses()
    {
        // Chỉ build EF model, không mở kết nối. Testcontainers bên integration test
        // kiểm tra chính SQL Server thực thi unique index.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new AppDbContext(options);

        var ticketEntity = db.Model.FindEntityType(typeof(InternalMaintenance.Maintenance.Domain.MaintenanceTicket))!;
        var index = ticketEntity.GetIndexes()
            .Single(candidate => candidate.GetDatabaseName() == "IX_MaintenanceTickets_EquipmentId_Active");
        var expectedFilter = $"[Status] IN ({string.Join(", ", TicketWorkflowRules.EquipmentOccupyingStatuses.Select(status => $"'{status}'"))})";

        Assert.True(index.IsUnique);
        Assert.Equal(expectedFilter, index.GetFilter());
    }

    [Fact]
    public void Refactor_does_not_change_relational_schema_against_last_migration_snapshot()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new AppDbContext(options);

        // Migration history vẫn nằm trong API host; DbContext chỉ chuyển sang Persistence.
        var migrationAssembly = typeof(InternalMaintenance.Api.Modules.Auth.AuthController).Assembly;
        var snapshotType = migrationAssembly
            .GetType("InternalMaintenance.Api.Migrations.AppDbContextModelSnapshot", throwOnError: true)!;
        var snapshot = (ModelSnapshot)Activator.CreateInstance(snapshotType, nonPublic: true)!;
        var snapshotModel = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model, designTime: true);
        var currentModel = db.GetService<IDesignTimeModel>().Model;
        var differ = db.GetService<IMigrationsModelDiffer>();
        var hasSchemaChanges = differ.HasDifferences(
            snapshotModel.GetRelationalModel(),
            currentModel.GetRelationalModel());

        Assert.False(hasSchemaChanges,
            "The refactor changed the SQL schema; review a generated migration before continuing.");
    }
}
