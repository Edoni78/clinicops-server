using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class NewInit7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "232e8504-48f8-4791-984d-5c1619ad419f", new DateTime(2026, 9, 5, 13, 47, 32, 903, DateTimeKind.Utc).AddTicks(8409), "AQAAAAIAAYagAAAAELluwWd8+k/dTGD3m5M6i3Ds0vDO8I2JdjI7tNa/ttzQQDVfIcAwcd/5aCm99sdEZQ==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 13, 47, 32, 942, DateTimeKind.Utc).AddTicks(5118));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "e5ef3c08-20a4-45e1-b738-1325d1432b08", new DateTime(2026, 9, 5, 13, 19, 21, 658, DateTimeKind.Utc).AddTicks(9920), "AQAAAAIAAYagAAAAEA60ZdH9vqmATD0L0k8jll7vG2ibPuItmKbXJgXvo+QQ5J9eRfiCXH4o9ccYrW/VWg==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 13, 19, 21, 694, DateTimeKind.Utc).AddTicks(2195));
        }
    }
}
