using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientCaseServiceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "PatientCases",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "c9caec78-d205-46d1-b2ba-169045766ecb", new DateTime(2026, 4, 7, 17, 37, 23, 774, DateTimeKind.Utc).AddTicks(8547), "AQAAAAIAAYagAAAAEOISMeRozykyP4FcO/QGQgoy/xC+/UM0GgliYIxFgZcwlwhWpZK3dxxSsj4danbKcA==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 7, 17, 37, 23, 810, DateTimeKind.Utc).AddTicks(6081));

            migrationBuilder.CreateIndex(
                name: "IX_PatientCases_ServiceId",
                table: "PatientCases",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientCases_Services_ServiceId",
                table: "PatientCases",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientCases_Services_ServiceId",
                table: "PatientCases");

            migrationBuilder.DropIndex(
                name: "IX_PatientCases_ServiceId",
                table: "PatientCases");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "PatientCases");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "9789dc79-6023-469c-b90f-73beac7e4f90", new DateTime(2026, 3, 24, 14, 22, 33, 192, DateTimeKind.Utc).AddTicks(5111), "AQAAAAIAAYagAAAAEN6CcQH+Ec88rzhdYWUXkC7+cOMLw2xtOAfer75ERS7o2mCgEqYwCYVJt2Wpk7adWA==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 14, 22, 33, 227, DateTimeKind.Utc).AddTicks(8181));
        }
    }
}
