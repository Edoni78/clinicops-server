using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorUserIdToMedicalReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoctorUserId",
                table: "MedicalReports",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoctorUserId",
                table: "MedicalReports");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "SuperAdmin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash" },
                values: new object[] { "af0bec78-1d86-4e4a-b209-ee99d2a23a0b", new DateTime(2026, 2, 6, 17, 55, 51, 516, DateTimeKind.Utc).AddTicks(9808), "AQAAAAIAAYagAAAAEH1BpAtltAnloR6xereKaVxQzNKibL1kN6t95VQc7HK0Lpye/MNOOn8U7rS/TUxUJw==" });

            migrationBuilder.UpdateData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 6, 17, 55, 51, 552, DateTimeKind.Utc).AddTicks(1617));
        }
    }
}
