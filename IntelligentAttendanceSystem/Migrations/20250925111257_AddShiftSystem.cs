using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IntelligentAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    ShiftId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    RelaxTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    OffTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.ShiftId);
                });

            migrationBuilder.CreateTable(
                name: "UserShifts",
                columns: table => new
                {
                    UserShiftId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserShifts", x => x.UserShiftId);
                    table.ForeignKey(
                        name: "FK_UserShifts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserShifts_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedDate", "PasswordHash" },
                values: new object[] { "7e3f72e9-5811-4e5e-b2a9-a0c3daa53f28", new DateTime(2025, 9, 25, 11, 12, 51, 294, DateTimeKind.Utc).AddTicks(6934), "AQAAAAIAAYagAAAAEOGYQlX2OIUWHhtmxpwLAa1o8Q8aeqQa7NkzfSUOvH0dfIe5BQ/AxSF0St+6s64maw==" });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "ShiftId", "CreatedDate", "Description", "IsActive", "OffTime", "RelaxTime", "ShiftCode", "ShiftName", "StartTime" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 25, 11, 12, 51, 294, DateTimeKind.Utc).AddTicks(7520), "Standard morning shift", true, new TimeSpan(0, 17, 0, 0, 0), new TimeSpan(0, 0, 15, 0, 0), "MORN", "Morning Shift", new TimeSpan(0, 9, 0, 0, 0) },
                    { 2, new DateTime(2025, 9, 25, 11, 12, 51, 294, DateTimeKind.Utc).AddTicks(7523), "Evening shift", true, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 0, 15, 0, 0), "EVEN", "Evening Shift", new TimeSpan(0, 14, 0, 0, 0) },
                    { 3, new DateTime(2025, 9, 25, 11, 12, 51, 294, DateTimeKind.Utc).AddTicks(7526), "Night shift", true, new TimeSpan(0, 6, 0, 0, 0), new TimeSpan(0, 0, 15, 0, 0), "NIGHT", "Night Shift", new TimeSpan(0, 22, 0, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ShiftId",
                table: "Attendances",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_ShiftCode",
                table: "Shifts",
                column: "ShiftCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserShifts_ShiftId",
                table: "UserShifts",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_UserShifts_UserId_EffectiveDate",
                table: "UserShifts",
                columns: new[] { "UserId", "EffectiveDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Shifts_ShiftId",
                table: "Attendances",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "ShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Shifts_ShiftId",
                table: "Attendances");

            migrationBuilder.DropTable(
                name: "UserShifts");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ShiftId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Attendances");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedDate", "PasswordHash" },
                values: new object[] { "f8422eae-2158-4340-a4ad-ee5449c2ca97", new DateTime(2025, 9, 24, 7, 50, 49, 700, DateTimeKind.Utc).AddTicks(1676), "AQAAAAIAAYagAAAAECv8mByc5rJrcu/ZS53SwgvgCaFHgkr8+hCV+v3MlPes3YUTZ0wjM+Xl30T5wljyvA==" });
        }
    }
}
