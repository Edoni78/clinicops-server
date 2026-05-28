using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class MfaAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "488cffb7-95d5-43c5-a930-8173efedaa0f", new DateTime(2026, 5, 28, 13, 26, 37, 80, DateTimeKind.Utc).AddTicks(8019), "AQAAAAIAAYagAAAAECDpzmccGfj8IvH2eFwl0s44ICSZi5+Neuvm90RU5YqhrsVkN6ZZY7h3mzO1cSDRbA==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 28, 13, 26, 37, 118, DateTimeKind.Utc).AddTicks(2914));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "e32cbe82-ba01-4a25-b818-8d3c85929516", new DateTime(2026, 5, 28, 12, 24, 33, 804, DateTimeKind.Utc).AddTicks(1976), "AQAAAAIAAYagAAAAEKt7cd5SMlLsrdS0wzgboEahoY6NkKnu/AOfPGWeSVJ7N+s+dBtPR1xEmJVg/HS47Q==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 28, 12, 24, 33, 838, DateTimeKind.Utc).AddTicks(6686));
        }
    }
}
