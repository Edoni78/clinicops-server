using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPatientCaseStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "1299eb6f-c8fe-457e-b62c-b56868fa4d47", new DateTime(2026, 5, 29, 5, 6, 48, 728, DateTimeKind.Utc).AddTicks(2283), "AQAAAAIAAYagAAAAEA2ulCs24Y2poSNyadGvsJIwIR77g5rGlgQztQOZ0vlJJToP47tEPh/ArJk/0d2kOg==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 29, 5, 6, 48, 767, DateTimeKind.Utc).AddTicks(1313));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "5e19df84-db23-4827-ac01-e6bb9cb878f4", new DateTime(2026, 5, 29, 4, 49, 48, 909, DateTimeKind.Utc).AddTicks(8397), "AQAAAAIAAYagAAAAELan/9MqnkZJIKua+hRtxFiJd+TzObGFF+g3itTscKtlzjWG+KV9FdhKpCbZqpyL1A==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 29, 4, 49, 48, 944, DateTimeKind.Utc).AddTicks(8362));
        }
    }
}
