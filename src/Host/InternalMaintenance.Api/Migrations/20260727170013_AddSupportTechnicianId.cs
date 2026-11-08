using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTechnicianId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupportTechnicianId",
                table: "MaintenanceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_SupportTechnicianId",
                table: "MaintenanceTickets",
                column: "SupportTechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_Users_SupportTechnicianId",
                table: "MaintenanceTickets",
                column: "SupportTechnicianId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_Users_SupportTechnicianId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_SupportTechnicianId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "SupportTechnicianId",
                table: "MaintenanceTickets");
        }
    }
}
