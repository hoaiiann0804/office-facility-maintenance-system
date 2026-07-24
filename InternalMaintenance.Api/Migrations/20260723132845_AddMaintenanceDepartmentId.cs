using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaintenanceDepartmentId",
                table: "Equipment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_MaintenanceDepartmentId",
                table: "Equipment",
                column: "MaintenanceDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Departments_MaintenanceDepartmentId",
                table: "Equipment",
                column: "MaintenanceDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Departments_MaintenanceDepartmentId",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_MaintenanceDepartmentId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "MaintenanceDepartmentId",
                table: "Equipment");
        }
    }
}
