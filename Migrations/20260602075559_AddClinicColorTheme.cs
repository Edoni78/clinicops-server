using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicColorTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColorTheme",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "e8c16ed8-d218-4c80-8bcf-7aefead6cfb8", new DateTime(2026, 6, 2, 7, 55, 59, 346, DateTimeKind.Utc).AddTicks(4535), "AQAAAAIAAYagAAAAEFvzYXSrQQQBtOqDrg+Z1uhnSduN/2L2NzWW64ZyzxPcMyGGVELQbgUPaYX8snWo+Q==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ColorTheme", "CreatedAt" },
                values: new object[] { 0, new DateTime(2026, 6, 2, 7, 55, 59, 381, DateTimeKind.Utc).AddTicks(3416) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorTheme",
                table: "Clinics");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "beede5c5-2aa5-4b68-a72c-424bdb7a665e", new DateTime(2026, 6, 1, 17, 2, 55, 214, DateTimeKind.Utc).AddTicks(1713), "AQAAAAIAAYagAAAAEPiMi+/6ujkxI1RuvXIR+I8nJjoNlHQGw31dDHISjrFe4OekZNZcgoIjRIxgYN+Olg==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 17, 2, 55, 249, DateTimeKind.Utc).AddTicks(3725));
        }
    }
}
