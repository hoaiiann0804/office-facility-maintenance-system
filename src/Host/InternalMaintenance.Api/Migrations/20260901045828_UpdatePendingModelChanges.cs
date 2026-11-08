using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_EquipmentId",
                table: "MaintenanceTickets");

            // Tự động dọn dẹp các ticket trùng lặp cũ (nếu có trên Production) trước khi tạo Unique Filtered Index
            migrationBuilder.Sql(@"
                WITH RankedTickets AS (
                    SELECT Id, ROW_NUMBER() OVER(PARTITION BY EquipmentId ORDER BY CreatedAt DESC) as rn
                    FROM MaintenanceTickets
                    WHERE [Status] IN ('Pending', 'Assigned', 'InProgress', 'Resolved', 'WaitingForVendor')
                )
                UPDATE MaintenanceTickets
                SET [Status] = 'Cancelled', CancellationReason = 'Auto-cancelled due to duplicate active ticket constraint migration'
                WHERE Id IN (SELECT Id FROM RankedTickets WHERE rn > 1);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_EquipmentId_Active",
                table: "MaintenanceTickets",
                column: "EquipmentId",
                unique: true,
                filter: "[Status] IN ('Pending', 'Assigned', 'InProgress', 'Resolved', 'WaitingForVendor')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_EquipmentId_Active",
                table: "MaintenanceTickets");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_EquipmentId",
                table: "MaintenanceTickets",
                column: "EquipmentId");
        }
    }
}
