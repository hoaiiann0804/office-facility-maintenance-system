using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations;

public partial class AddSlaOutboxFailureState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "FailedAt",
            table: "SlaNotificationOutboxMessages",
            type: "datetime2",
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_SlaNotificationOutbox_Pending",
            table: "SlaNotificationOutboxMessages");

        migrationBuilder.CreateIndex(
            name: "IX_SlaNotificationOutbox_Pending",
            table: "SlaNotificationOutboxMessages",
            columns: new[] { "NextAttemptAt", "LeaseUntil" },
            filter: "[ProcessedAt] IS NULL AND [FailedAt] IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SlaNotificationOutbox_Pending",
            table: "SlaNotificationOutboxMessages");

        migrationBuilder.DropColumn(
            name: "FailedAt",
            table: "SlaNotificationOutboxMessages");

        migrationBuilder.CreateIndex(
            name: "IX_SlaNotificationOutbox_Pending",
            table: "SlaNotificationOutboxMessages",
            columns: new[] { "NextAttemptAt", "LeaseUntil" },
            filter: "[ProcessedAt] IS NULL");
    }
}
