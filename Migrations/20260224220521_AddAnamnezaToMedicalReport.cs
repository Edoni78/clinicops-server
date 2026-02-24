using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class AddAnamnezaToMedicalReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Anamneza",
                table: "MedicalReports",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "6fe235a4-4b72-49f1-8f78-0ed12ba60f0e", new DateTime(2026, 2, 24, 22, 5, 21, 269, DateTimeKind.Utc).AddTicks(9796), "AQAAAAIAAYagAAAAEL7foHpASyUCJJYVTL2yTuKrizxrBAD1OAAUOu3/85dbkLU41/M60BozKZx0kmr6Qg==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 24, 22, 5, 21, 306, DateTimeKind.Utc).AddTicks(7281));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anamneza",
                table: "MedicalReports");

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
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 20, 20, 18, 239, DateTimeKind.Utc).AddTicks(3063));
        }
    }
}
