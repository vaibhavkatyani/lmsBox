using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsbox.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateId",
                table: "LearnerProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CertificateIssuedAt",
                table: "LearnerProgresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateIssuedBy",
                table: "LearnerProgresses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateId",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "CertificateIssuedAt",
                table: "LearnerProgresses");

            migrationBuilder.DropColumn(
                name: "CertificateIssuedBy",
                table: "LearnerProgresses");
        }
    }
}
