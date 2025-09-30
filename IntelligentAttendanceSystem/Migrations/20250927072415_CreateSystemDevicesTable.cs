using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelligentAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class CreateSystemDevicesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IPVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    SubnetMask = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    GatewayMacAddress = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetailType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HttpPort = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemDevices", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 27, 7, 24, 9, 603, DateTimeKind.Utc).AddTicks(3031));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 27, 7, 24, 9, 603, DateTimeKind.Utc).AddTicks(3033));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 27, 7, 24, 9, 603, DateTimeKind.Utc).AddTicks(3066));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemDevices");

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 25, 15, 14, 31, 71, DateTimeKind.Utc).AddTicks(5080));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 25, 15, 14, 31, 71, DateTimeKind.Utc).AddTicks(5083));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 25, 15, 14, 31, 71, DateTimeKind.Utc).AddTicks(5085));
        }
    }
}
