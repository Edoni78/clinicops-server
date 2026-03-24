using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class NewFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClinicMode",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClinicMode",
                table: "ClinicApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                columns: new[] { "ClinicMode", "CreatedAt" },
                values: new object[] { 1, new DateTime(2026, 3, 24, 14, 22, 33, 227, DateTimeKind.Utc).AddTicks(8181) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClinicMode",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "ClinicMode",
                table: "ClinicApplications");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "77534497-8aa6-47ea-b6a3-230468ca5a00", new DateTime(2026, 3, 10, 22, 20, 14, 643, DateTimeKind.Utc).AddTicks(3303), "AQAAAAIAAYagAAAAEHCHKhZDijyAv1lSnqtHv4jGmcpAIrIKUmknmkDym7P/x8k1gBGTF3OoT734TpM1OQ==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 10, 22, 20, 14, 682, DateTimeKind.Utc).AddTicks(7859));
        }
    }
}
