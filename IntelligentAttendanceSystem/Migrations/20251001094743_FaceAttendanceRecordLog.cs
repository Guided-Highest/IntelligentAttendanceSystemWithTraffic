using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelligentAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class FaceAttendanceRecordLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaceAttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Similarity = table.Column<float>(type: "real", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FaceImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CandidateImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GlobalImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceAttendanceRecords", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceRecords_EventId",
                table: "FaceAttendanceRecords",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceRecords_EventTime",
                table: "FaceAttendanceRecords",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceRecords_UserId",
                table: "FaceAttendanceRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceAttendanceRecords");

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 30, 5, 38, 46, 932, DateTimeKind.Utc).AddTicks(8895));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 30, 5, 38, 46, 932, DateTimeKind.Utc).AddTicks(8899));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 30, 5, 38, 46, 932, DateTimeKind.Utc).AddTicks(8900));
        }
    }
}
