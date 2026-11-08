using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BreachedNotifiedAt",
                table: "MaintenanceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "MaintenanceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedNotifiedAt",
                table: "MaintenanceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NearBreachNotifiedAt",
                table: "MaintenanceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlaStatus",
                table: "MaintenanceTickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreachedNotifiedAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "EscalatedNotifiedAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "NearBreachNotifiedAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "SlaStatus",
                table: "MaintenanceTickets");
        }
    }
}
