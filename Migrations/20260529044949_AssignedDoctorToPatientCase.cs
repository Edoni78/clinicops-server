using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class AssignedDoctorToPatientCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDoctorUserId",
                table: "PatientCases",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.CreateIndex(
                name: "IX_PatientCases_AssignedDoctorUserId",
                table: "PatientCases",
                column: "AssignedDoctorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientCases_AspNetUsers_AssignedDoctorUserId",
                table: "PatientCases",
                column: "AssignedDoctorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientCases_AspNetUsers_AssignedDoctorUserId",
                table: "PatientCases");

            migrationBuilder.DropIndex(
                name: "IX_PatientCases_AssignedDoctorUserId",
                table: "PatientCases");

            migrationBuilder.DropColumn(
                name: "AssignedDoctorUserId",
                table: "PatientCases");

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
    }
}
