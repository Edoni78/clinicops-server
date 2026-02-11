using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorUserIdToMedicalReport2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "cfd4207b-f54a-4ed7-ae44-1201a1e763ba", new DateTime(2026, 2, 11, 15, 24, 47, 909, DateTimeKind.Utc).AddTicks(5045), "AQAAAAIAAYagAAAAEPK4wKDvwuIHFh8lNmsa76/r8IcdZOdTXjU00+JdUgbMTmlAzqNsCamyn9enDiFeew==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 11, 15, 24, 47, 944, DateTimeKind.Utc).AddTicks(85));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "50a01053-8e5c-40bb-8546-ea753660009a", new DateTime(2026, 2, 11, 14, 1, 38, 296, DateTimeKind.Utc).AddTicks(6776), "AQAAAAIAAYagAAAAENBpgpz29ZW+AllrNjnR8658AeuMjnWzYhi5RtBHlslWctVu/JaGN3Sgm7k9bbX+hg==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 11, 14, 1, 38, 334, DateTimeKind.Utc).AddTicks(4673));
        }
    }
}
