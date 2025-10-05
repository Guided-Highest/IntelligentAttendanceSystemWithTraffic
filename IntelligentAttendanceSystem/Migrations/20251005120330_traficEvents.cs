using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelligentAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class traficEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trafficRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Speed = table.Column<float>(type: "real", nullable: false),
                    ViolationType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ViolationDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: true),
                    JunctionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LaneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GlobalImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VehicleImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlateImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trafficRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimePeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    JunctionId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCounts", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 5, 12, 3, 24, 729, DateTimeKind.Utc).AddTicks(9847));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 5, 12, 3, 24, 729, DateTimeKind.Utc).AddTicks(9851));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 5, 12, 3, 24, 729, DateTimeKind.Utc).AddTicks(9854));

            migrationBuilder.CreateIndex(
                name: "IX_trafficRecords_EventId",
                table: "trafficRecords",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trafficRecords_EventTime",
                table: "trafficRecords",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_trafficRecords_JunctionId",
                table: "trafficRecords",
                column: "JunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_trafficRecords_PlateNumber",
                table: "trafficRecords",
                column: "PlateNumber");

            migrationBuilder.CreateIndex(
                name: "IX_trafficRecords_ViolationType",
                table: "trafficRecords",
                column: "ViolationType");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCounts_CountDate",
                table: "VehicleCounts",
                column: "CountDate");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCounts_CountDate_VehicleType_Direction_JunctionId",
                table: "VehicleCounts",
                columns: new[] { "CountDate", "VehicleType", "Direction", "JunctionId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCounts_JunctionId",
                table: "VehicleCounts",
                column: "JunctionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trafficRecords");

            migrationBuilder.DropTable(
                name: "VehicleCounts");

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 3, 7, 55, 24, 320, DateTimeKind.Utc).AddTicks(3691));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 3, 7, 55, 24, 320, DateTimeKind.Utc).AddTicks(3694));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 3, 7, 55, 24, 320, DateTimeKind.Utc).AddTicks(3696));
        }
    }
}
