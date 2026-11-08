using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentStorageCleanupLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StorageDeleteLeaseId",
                table: "TicketAttachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StorageDeleteLeaseUntil",
                table: "TicketAttachments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageDeleteLeaseId",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "StorageDeleteLeaseUntil",
                table: "TicketAttachments");
        }
    }
}
