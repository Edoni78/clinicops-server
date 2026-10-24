using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class NewwInit9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "f0527467-0528-4257-9419-97bf627cf6f7", new DateTime(2026, 10, 24, 13, 8, 28, 198, DateTimeKind.Utc).AddTicks(7843), "AQAAAAIAAYagAAAAEHXKonvmjZkJ7IONrlVthnhDiLzExFAWnfKT0sn+RXUPh67RBzk17QgByD0j1cL2wg==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 24, 13, 8, 28, 239, DateTimeKind.Utc).AddTicks(1208));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "27cdba0b-ef11-46ff-ad2c-c2b1e379422a", new DateTime(2026, 10, 24, 12, 53, 37, 933, DateTimeKind.Utc).AddTicks(4351), "AQAAAAIAAYagAAAAENiwKfYOG9oX7FYApOlsTni8cJHM576Rx9BHOoy4eeSkhjR0DEfSKiHWJYOhd1v6wg==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 24, 12, 53, 37, 969, DateTimeKind.Utc).AddTicks(9430));
        }
    }
}
