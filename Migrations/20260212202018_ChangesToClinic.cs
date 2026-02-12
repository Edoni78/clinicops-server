using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class ChangesToClinic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Clinics",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Clinics",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "52538362-aaac-49ee-8328-3e9a390fb973", new DateTime(2026, 2, 12, 20, 20, 18, 203, DateTimeKind.Utc).AddTicks(8885), "AQAAAAIAAYagAAAAEJqMXXcZZWAuX9pnR0EmDWQZG3gjYAjo+w6vpNMKZEXLq+TxzpwHx24Uv6abl5oVEA==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Description", "LogoUrl" },
                values: new object[] { new DateTime(2026, 2, 12, 20, 20, 18, 239, DateTimeKind.Utc).AddTicks(3063), null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Clinics");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "ec3fce85-96bb-4a78-b0f6-fd536b2242cc", new DateTime(2026, 2, 11, 21, 7, 15, 921, DateTimeKind.Utc).AddTicks(4625), "AQAAAAIAAYagAAAAEIp8wM9yEnFthXnD+/EKpSPTa1ERqPTnNqm/9eAqZ5KB5vQawqG0Md4a/U4c3SP68A==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 11, 21, 7, 15, 959, DateTimeKind.Utc).AddTicks(2319));
        }
    }
}
