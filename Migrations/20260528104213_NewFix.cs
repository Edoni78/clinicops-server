using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class NewFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "9089ac74-d4f7-4864-ab59-2c96acb1b79a", new DateTime(2026, 5, 28, 10, 42, 12, 937, DateTimeKind.Utc).AddTicks(5208), "AQAAAAIAAYagAAAAEI8/0FG8znWhaVoU5dMbObUdJDdzW3hQjEFSUZxs0qVPpk4RprH7/Mmhogsgnz/HUw==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 28, 10, 42, 12, 972, DateTimeKind.Utc).AddTicks(3216));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "1e5575e8-35d9-42ee-a003-3d8b7acb1bc7", new DateTime(2026, 5, 28, 10, 36, 47, 905, DateTimeKind.Utc).AddTicks(1482), "AQAAAAIAAYagAAAAEEBWnBTJi5exVlnL2XFlpqtilQ7a/SZ9/xXsmfPNWrQkbvMnbp9of3f2yJswgWBMtw==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 5, 28, 10, 36, 47, 940, DateTimeKind.Utc).AddTicks(3195));
        }
    }
}
