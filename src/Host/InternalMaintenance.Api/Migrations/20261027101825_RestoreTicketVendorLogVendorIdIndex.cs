using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class RestoreTicketVendorLogVendorIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TicketVendorLogs_VendorId",
                table: "TicketVendorLogs",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketVendorLogs_VendorId",
                table: "TicketVendorLogs");
        }
    }
}
