using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelligentAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class DiffTwoColDateWayAndMAC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GatewayMacAddress",
                table: "SystemDevices");

            migrationBuilder.AlterColumn<string>(
                name: "SubnetMask",
                table: "SystemDevices",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "IPVersion",
                table: "SystemDevices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DetailType",
                table: "SystemDevices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Gateway",
                table: "SystemDevices",
                type: "nvarchar(17)",
                maxLength: 17,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                table: "SystemDevices",
                type: "nvarchar(17)",
                maxLength: 17,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 29, 10, 27, 30, 157, DateTimeKind.Utc).AddTicks(5391));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 29, 10, 27, 30, 157, DateTimeKind.Utc).AddTicks(5394));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 29, 10, 27, 30, 157, DateTimeKind.Utc).AddTicks(5396));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gateway",
                table: "SystemDevices");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                table: "SystemDevices");

            migrationBuilder.AlterColumn<string>(
                name: "SubnetMask",
                table: "SystemDevices",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IPVersion",
                table: "SystemDevices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DetailType",
                table: "SystemDevices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayMacAddress",
                table: "SystemDevices",
                type: "nvarchar(17)",
                maxLength: 17,
                nullable: false,
                defaultValue: "");

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
    }
}
