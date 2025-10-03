using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelligentAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class RelationFaceUserAndAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_AspNetUsers_UserId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_FaceUsers_UserId",
                table: "FaceUsers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FaceUsers");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "DeviceUserId",
                table: "FaceUsers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "FaceUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Attendances",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FaceUsers_DeviceUserId",
                table: "FaceUsers",
                column: "DeviceUserId");

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 3, 7, 36, 25, 601, DateTimeKind.Utc).AddTicks(3881));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 3, 7, 36, 25, 601, DateTimeKind.Utc).AddTicks(3884));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 3, 7, 36, 25, 601, DateTimeKind.Utc).AddTicks(3886));

            migrationBuilder.CreateIndex(
                name: "IX_FaceUsers_DeviceUserId",
                table: "FaceUsers",
                column: "DeviceUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ApplicationUserId",
                table: "Attendances",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_AspNetUsers_ApplicationUserId",
                table: "Attendances",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_FaceUsers_UserId",
                table: "Attendances",
                column: "UserId",
                principalTable: "FaceUsers",
                principalColumn: "DeviceUserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_AspNetUsers_ApplicationUserId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_FaceUsers_UserId",
                table: "Attendances");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FaceUsers_DeviceUserId",
                table: "FaceUsers");

            migrationBuilder.DropIndex(
                name: "IX_FaceUsers_DeviceUserId",
                table: "FaceUsers");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ApplicationUserId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "DeviceUserId",
                table: "FaceUsers");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "FaceUsers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Attendances");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FaceUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 2, 16, 39, 35, 380, DateTimeKind.Utc).AddTicks(9300));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 2, 16, 39, 35, 380, DateTimeKind.Utc).AddTicks(9303));

            migrationBuilder.UpdateData(
                table: "Shifts",
                keyColumn: "ShiftId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 2, 16, 39, 35, 380, DateTimeKind.Utc).AddTicks(9305));

            migrationBuilder.CreateIndex(
                name: "IX_FaceUsers_UserId",
                table: "FaceUsers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_AspNetUsers_UserId",
                table: "Attendances",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
