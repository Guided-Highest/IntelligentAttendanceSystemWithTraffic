using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelligentAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class FaceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "FaceAttendanceRecords",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "FaceAttendanceRecords",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "FaceAttendanceRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "FaceAttendanceRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "FaceAttendanceRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "FaceAttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FaceUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FaceImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeviceGroupId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceGroupName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceUsers", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 1, 12, 26, 23, 971, DateTimeKind.Utc).AddTicks(4239));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 1, 12, 26, 23, 971, DateTimeKind.Utc).AddTicks(4241));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 1, 12, 26, 23, 971, DateTimeKind.Utc).AddTicks(4243));

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceRecords_Department",
                table: "FaceAttendanceRecords",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceRecords_UserId1",
                table: "FaceAttendanceRecords",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_FaceUsers_Department",
                table: "FaceUsers",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_FaceUsers_IsActive",
                table: "FaceUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FaceUsers_Name",
                table: "FaceUsers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FaceUsers_UserId",
                table: "FaceUsers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FaceAttendanceRecords_FaceUsers_UserId1",
                table: "FaceAttendanceRecords",
                column: "UserId1",
                principalTable: "FaceUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaceAttendanceRecords_FaceUsers_UserId1",
                table: "FaceAttendanceRecords");

            migrationBuilder.DropTable(
                name: "FaceUsers");

            migrationBuilder.DropIndex(
                name: "IX_FaceAttendanceRecords_Department",
                table: "FaceAttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_FaceAttendanceRecords_UserId1",
                table: "FaceAttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "FaceAttendanceRecords");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "FaceAttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "FaceAttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "FaceAttendanceRecords");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "FaceAttendanceRecords");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "FaceAttendanceRecords",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 1, 9, 47, 38, 483, DateTimeKind.Utc).AddTicks(5039));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 1, 9, 47, 38, 483, DateTimeKind.Utc).AddTicks(5041));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 1, 9, 47, 38, 483, DateTimeKind.Utc).AddTicks(5043));
        }
    }
}
