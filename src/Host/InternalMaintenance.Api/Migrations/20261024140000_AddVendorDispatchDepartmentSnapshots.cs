using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalMaintenance.Api.Migrations;

public partial class AddVendorDispatchDepartmentSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DepartmentIdAtDispatch",
            table: "TicketVendorLogs",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MaintenanceDepartmentIdAtDispatch",
            table: "TicketVendorLogs",
            type: "int",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DepartmentIdAtDispatch", table: "TicketVendorLogs");
        migrationBuilder.DropColumn(name: "MaintenanceDepartmentIdAtDispatch", table: "TicketVendorLogs");
    }
}
