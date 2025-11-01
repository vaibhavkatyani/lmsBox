using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsbox.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedEmailSendingLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastSendError",
                table: "LoginLinkTokens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SendFailedCount",
                table: "LoginLinkTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "LoginLinkTokens",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSendError",
                table: "LoginLinkTokens");

            migrationBuilder.DropColumn(
                name: "SendFailedCount",
                table: "LoginLinkTokens");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "LoginLinkTokens");
        }
    }
}
