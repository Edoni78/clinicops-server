using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicops.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorProfileToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoctorDisplayName",
                table: "AspNetUsers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SignatureUrl",
                table: "AspNetUsers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StampUrl",
                table: "AspNetUsers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DoctorDisplayName", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "SignatureUrl", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "StampUrl", table: "AspNetUsers");
        }
    }
}
