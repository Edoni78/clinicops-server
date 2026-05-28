using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class SomeModelChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "59c00a33-1c81-4373-a77b-f68adacc2f98", new DateTime(2026, 5, 26, 12, 25, 22, 703, DateTimeKind.Utc).AddTicks(8269), "AQAAAAIAAYagAAAAEPrQeAgojyeM8n04OU3stM0lJp/1GJrRZ4kYre+7jJSNKOUQwwePD+40oV2SBJyxIQ==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 12, 25, 22, 738, DateTimeKind.Utc).AddTicks(2370));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "a8be5ed3-0a4c-416a-8361-5d887ee1a1cc", new DateTime(2026, 5, 25, 14, 23, 16, 554, DateTimeKind.Utc).AddTicks(3152), "AQAAAAIAAYagAAAAENSAN+oYvU96wuFF4pwf7quX1y0dx+GgomZl1g66INF/WRp0yyReaQaX/a9FovTD8A==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 25, 14, 23, 16, 589, DateTimeKind.Utc).AddTicks(1626));
        }
    }
}
