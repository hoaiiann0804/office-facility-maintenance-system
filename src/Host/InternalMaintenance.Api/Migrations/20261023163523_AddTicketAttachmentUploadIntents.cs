using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketAttachmentUploadIntents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketAttachmentUploadIntents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadUrlExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StorageDeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CleanupLeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CleanupLeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAttachmentUploadIntents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachmentUploadIntents_PendingCleanup",
                table: "TicketAttachmentUploadIntents",
                columns: new[] { "UploadUrlExpiresAt", "CleanupLeaseUntil" },
                filter: "[ConfirmedAt] IS NULL AND [StorageDeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachmentUploadIntents_StorageKey",
                table: "TicketAttachmentUploadIntents",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketAttachmentUploadIntents");
        }
    }
}
