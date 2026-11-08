using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentStorageDeletionQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StorageDeletedAt",
                table: "TicketAttachments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_PendingStorageDeletion",
                table: "TicketAttachments",
                column: "DeletedAt",
                filter: "[IsDeleted] = 1 AND [StorageDeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketAttachments_PendingStorageDeletion",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "StorageDeletedAt",
                table: "TicketAttachments");
        }
    }
}
