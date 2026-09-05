using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class NewInit4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "6c700439-5006-461f-b07c-46545ea95eb4", new DateTime(2026, 9, 5, 13, 16, 23, 314, DateTimeKind.Utc).AddTicks(6460), "AQAAAAIAAYagAAAAEAzDbiK2+Kr1cLWLB5b4PoweAqn8IngkHvWLfm1p4MaWODe/nf/CXk9rNovx9jJDkA==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 13, 16, 23, 352, DateTimeKind.Utc).AddTicks(1666));
        }
    }
}
