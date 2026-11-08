using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVendorCrossModuleForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketVendorLogs_Vendors_VendorId",
                table: "TicketVendorLogs");

            migrationBuilder.DropIndex(
                name: "IX_TicketVendorLogs_VendorId",
                table: "TicketVendorLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TicketVendorLogs_VendorId",
                table: "TicketVendorLogs",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketVendorLogs_Vendors_VendorId",
                table: "TicketVendorLogs",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
