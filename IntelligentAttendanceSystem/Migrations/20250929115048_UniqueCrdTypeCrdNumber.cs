using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelligentAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class UniqueCrdTypeCrdNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 29, 11, 50, 45, 436, DateTimeKind.Utc).AddTicks(5767));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 29, 11, 50, 45, 436, DateTimeKind.Utc).AddTicks(5771));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 9, 29, 11, 50, 45, 436, DateTimeKind.Utc).AddTicks(5774));

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CredentialType_CredentialNumber",
                table: "AspNetUsers",
                columns: new[] { "CredentialType", "CredentialNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CredentialType_CredentialNumber",
                table: "AspNetUsers");

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
    }
}
